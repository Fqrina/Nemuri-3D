using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BossFightManager : MonoBehaviour
{
    public static BossFightManager Instance { get; private set; }

    [Header("Settings")]
    public float orbSpawnInterval = 60f;
    public float kaelDmgMultiplierIncrease = 0.50f;

    [Header("State")]
    public float damageMultiplier = 1.0f;
    public int orbsCollected = 0;

    // Cooldown trackers: Index maps to Character Index in CharacterSwapManager
    // 2: Murial (30s), 3: Keiko (40s), 4: Feanor (30s)
    private float[] cooldownTimers = new float[5];
    private float orbSpawnTimer = 0f;

    // UI References
    private Canvas canvas;
    private Text hudText;

    // Minigame states
    private bool isMinigameActive = false;
    private float minigameTimer = 0f;
    private int currentMinigameChar = -1;

    // Murial Mash minigame data
    private int mashCount = 0;
    private float mashSliderValue = 0f;
    private GameObject mashPanel;
    private RectTransform mashFillRect;

    // Feanor Osu! minigame data
    private int osuHitCount = 0;
    private GameObject osuPanel;
    private GameObject currentOsuTarget;

    // References to Boss and Player
    private Transform bossTransform;
    private Transform playerTransform;
    private Transform mapCenterTransform;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        
        // Dynamic EventSystem creation/fix to allow UI clicks to work in Chapter 3
        UnityEngine.EventSystems.EventSystem activeES = FindFirstObjectByTypeAll<UnityEngine.EventSystems.EventSystem>();
        if (activeES == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            activeES = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        if (activeES != null)
        {
            System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                if (activeES.GetComponent(inputModuleType) == null)
                {
                    activeES.gameObject.AddComponent(inputModuleType);
                    Debug.Log("[BossFightManager] Added InputSystemUIInputModule to EventSystem dynamically.");
                }
                var oldModule = activeES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null) Destroy(oldModule);
            }
            else
            {
                if (activeES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
                {
                    activeES.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log("[BossFightManager] Added StandaloneInputModule to EventSystem dynamically.");
                }
            }
        }

        CreateHUDUI();
    }

    private void Start()
    {
        FindEntities();
        SetupCameraObstructionAndLayers();
        orbSpawnTimer = orbSpawnInterval - 5f; // Spawn the first orb 5 seconds into the fight
    }

    private void SetupCameraObstructionAndLayers()
    {
        // 1. Ensure active Main Camera has CameraObstructionManager component
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<Nemuri.CameraEffects.CameraObstructionManager>() == null)
        {
            mainCam.gameObject.AddComponent<Nemuri.CameraEffects.CameraObstructionManager>();
            Debug.Log("[BossFightManager] Added CameraObstructionManager to Camera.main.");
        }

        // 2. Resolve target layer index (CameraDissappear or CameraDisappear)
        int disappearLayer = LayerMask.NameToLayer("CameraDissappear");
        if (disappearLayer == -1) disappearLayer = LayerMask.NameToLayer("CameraDisappear");

        if (disappearLayer == -1)
        {
            Debug.LogWarning("[BossFightManager] Neither 'CameraDissappear' nor 'CameraDisappear' layer was found in project.");
            return;
        }

        // 3. Assign highlighted objects from hierarchy to target layer
        string[] targetObjectNames = new string[]
        {
            "Cube.003",
            "Cylinder.001", "Cylinder.002", "Cylinder.003",
            "Cylinder.010", "Cylinder.011", "Cylinder.012", "Cylinder.013",
            "Cylinder.014", "Cylinder.015", "Cylinder.016",
            "Sphere"
        };

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || !obj.scene.isLoaded) continue;

            foreach (string targetName in targetObjectNames)
            {
                if (obj.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    SetLayerRecursive(obj, disappearLayer);
                    Debug.Log("[BossFightManager] Configured object '" + obj.name + "' to layer " + LayerMask.LayerToName(disappearLayer));
                    break;
                }
            }
        }

    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursive(child.gameObject, layer);
        }
    }

    private void Update()
    {
        FindEntities();

        // 1. Update cooldown timers
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0f)
            {
                cooldownTimers[i] -= Time.deltaTime;
            }
        }

        // 2. Kael Orb spawning logic (every 60s)
        if (bossTransform != null || mapCenterTransform != null)
        {
            orbSpawnTimer += Time.deltaTime;
            if (orbSpawnTimer >= orbSpawnInterval)
            {
                orbSpawnTimer = 0f;
                SpawnGreenOrb();
            }
        }

        // 3. Update HUD Display
        UpdateHUDText();

        // 4. Ability activation check
        if (!isMinigameActive)
        {
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            {
                TryActivateAbility();
            }
        }
        else
        {
            // Run active minigame update loops
            if (currentMinigameChar == 2) // Murial mashing
            {
                UpdateMurialMinigame();
            }
            else if (currentMinigameChar == 4) // Feanor osu-clicking
            {
                UpdateFeanorMinigame();
            }
        }
    }

    private void FindEntities()
    {
        // Find player
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // Find boss
        if (bossTransform == null)
        {
            GameObject bossObj = GameObject.Find("EVILRABBIT");
            if (bossObj == null) bossObj = GameObject.Find("EVILRABBIT(Clone)");
            if (bossObj == null)
            {
                HoverAnimationController hover = FindFirstObjectByTypeAll<HoverAnimationController>();
                if (hover != null) bossObj = hover.gameObject;
            }
            if (bossObj != null) bossTransform = bossObj.transform;
        }

        // Find AMYGDALA map center
        if (mapCenterTransform == null)
        {
            GameObject mapObj = GameObject.Find("AMYGDALA");
            if (mapObj == null) mapObj = GameObject.Find("AMYGDALA(Clone)");
            if (mapObj == null) mapObj = FindSceneObjectContaining("AMYGDALA");
            if (mapObj != null) mapCenterTransform = mapObj.transform;
        }
    }

    private GameObject FindSceneObjectContaining(string name)
    {
        GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objs)
        {
            if (obj.name.Contains(name) && obj.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }

    private void TryActivateAbility()
    {
        if (Nemuri.Core.CharacterSwapManager.Instance == null) return;

        int activeIdx = Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex;

        // Check if ability is on cooldown
        if (cooldownTimers[activeIdx] > 0f)
        {
            Debug.Log("[BossFightManager] Ability on cooldown! Wait " + cooldownTimers[activeIdx].ToString("F1") + "s");
            return;
        }

        switch (activeIdx)
        {
            case 2: // Murial: Mash Minigame
                StartMurialMinigame();
                break;
            case 3: // Keiko: Heal
                ActivateKeikoHeal();
                break;
            case 4: // Feanor: Osu! Minigame
                StartFeanorMinigame();
                break;
        }
    }

    // ==========================================
    // KAEL'S ORB SYSTEM
    // ==========================================

    public void CollectOrb()
    {
        orbsCollected++;
        damageMultiplier += kaelDmgMultiplierIncrease;
        Debug.Log("[BossFightManager] Kael collected an orb! Multiplier: " + damageMultiplier.ToString("F2") + "x");
    }

    private void SpawnGreenOrb()
    {
        Transform centerTransform = mapCenterTransform != null ? mapCenterTransform : (bossTransform != null ? bossTransform : playerTransform);
        if (centerTransform == null) return;

        float groundY = playerTransform != null ? playerTransform.position.y : centerTransform.position.y;
        Vector3 spawnPos = centerTransform.position + new Vector3(Random.Range(-22f, 22f), 0f, Random.Range(-22f, 22f));
        spawnPos.y = groundY + 1.8f; // Floating at a slightly higher chest height

        GameObject orb = new GameObject("KaelOrb");
        orb.transform.position = spawnPos;
        orb.AddComponent<KaelOrb>();
        Debug.Log("[BossFightManager] Yellow orb spawned at: " + spawnPos + " (Center: " + centerTransform.name + ")");
    }

    // ==========================================
    // KEIKO'S HEAL ABILITY
    // ==========================================

    private void ActivateKeikoHeal()
    {
        if (playerTransform == null) return;

        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.Heal(10f);
            cooldownTimers[3] = 40f; // 40 seconds cooldown
            Debug.Log("[BossFightManager] Keiko casted Heal! Recalculating HP...");

            // Play green healing ring/burst around player
            GameObject healRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            healRing.transform.position = playerTransform.position;
            healRing.transform.localScale = new Vector3(3f, 0.1f, 3f);
            Destroy(healRing.GetComponent<Collider>());

            Material healMat = new Material(Shader.Find("Sprites/Default"));
            healMat.color = new Color(0.1f, 0.9f, 0.4f, 0.4f);
            healRing.GetComponent<MeshRenderer>().material = healMat;
            
            Destroy(healRing, 0.8f);
            healRing.AddComponent<HealRingAnimator>();
        }
    }

    // ==========================================
    // MURIAL'S MASH MINIGAME
    // ==========================================

    private void StartMurialMinigame()
    {
        isMinigameActive = true;
        currentMinigameChar = 2;
        minigameTimer = 5f;
        mashCount = 0;
        mashSliderValue = 0f;

        // Lock player movement
        if (Nemuri.Player.PlayerMovement.Instance != null)
        {
            Nemuri.Player.PlayerMovement.Instance.SetCanMove(false);
        }

        CreateMashPanel();
    }

    private void UpdateMurialMinigame()
    {
        minigameTimer -= Time.deltaTime;

        // Decay mash bar over time
        mashSliderValue = Mathf.Max(0f, mashSliderValue - 1.5f * Time.deltaTime);
        if (mashFillRect != null)
        {
            Vector2 anchorMax = mashFillRect.anchorMax;
            anchorMax.x = Mathf.Clamp01(mashSliderValue / 25f);
            mashFillRect.anchorMax = anchorMax;
        }

        // Detect Mash input
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            mashCount++;
            mashSliderValue = Mathf.Min(25f, mashSliderValue + 1.2f);
        }

        // End condition
        if (minigameTimer <= 0f)
        {
            EndMurialMinigame();
        }
    }

    private void EndMurialMinigame()
    {
        isMinigameActive = false;
        currentMinigameChar = -1;

        if (mashPanel != null) Destroy(mashPanel);

        // Unlock player movement
        if (Nemuri.Player.PlayerMovement.Instance != null)
        {
            Nemuri.Player.PlayerMovement.Instance.SetCanMove(true);
        }

        // Deal damage (max 5 base damage without Kael boost, boosted by Kael multiplier)
        float rawDamage = mashCount * 1.5f;
        float baseDamage = Mathf.Min(5f, rawDamage);
        float finalDamage = baseDamage * damageMultiplier;
        Debug.Log("[BossFightManager] Murial Mash Minigame ended! Score: " + mashCount + ". Base Dmg: " + baseDamage + ", Final Dmg: " + finalDamage);

        // Trigger dynamic visual effect (Rocks smash)
        TriggerMurialRocksVisual(finalDamage);

        cooldownTimers[2] = 30f; // 30s cooldown
    }

    private GameObject CreateRockObject(string name)
    {
        Mesh rockMesh = null;
        Material[] rockMaterials = null;

#if UNITY_EDITOR
        // Extract the rock.001 mesh directly from the FBX file to avoid parent offset shifts
        GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Maps/CHAPT1/PINEALGLANDrev.fbx");
        if (fbx != null)
        {
            foreach (Transform t in fbx.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("rock.001"))
                {
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    MeshRenderer mr = t.GetComponent<MeshRenderer>();
                    if (mf != null && mr != null)
                    {
                        rockMesh = mf.sharedMesh;
                        rockMaterials = mr.sharedMaterials;
                        break;
                    }
                }
            }
        }
#endif

        // Fallback: Find in memory meshes
        if (rockMesh == null)
        {
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
            foreach (Mesh m in meshes)
            {
                if (m.name.ToLower().Contains("rock.001") || m.name.ToLower().Contains("rock"))
                {
                    rockMesh = m;
                    break;
                }
            }
        }

        if (rockMesh != null)
        {
            GameObject rockGo = new GameObject(name);
            rockGo.AddComponent<MeshFilter>().sharedMesh = rockMesh;
            MeshRenderer mr = rockGo.AddComponent<MeshRenderer>();
            if (rockMaterials != null)
            {
                mr.sharedMaterials = rockMaterials;
            }
            return rockGo;
        }

        // Final fallback: Sphere primitive
        GameObject fallbackGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallbackGo.name = name;
        Collider col = fallbackGo.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return fallbackGo;
    }

    private void PrepareRockMaterial(GameObject rock)
    {
        MeshRenderer[] renderers = rock.GetComponentsInChildren<MeshRenderer>(true);
        Material rockMat = new Material(Shader.Find("Sprites/Default"));
        rockMat.color = new Color(0.35f, 0.3f, 0.28f, 0f); // transparent start
        foreach (MeshRenderer mr in renderers)
        {
            Texture mainTex = mr.material != null ? mr.material.mainTexture : null;
            if (mainTex != null)
            {
                rockMat.mainTexture = mainTex;
            }
            mr.material = rockMat;
        }
    }

    private void SetRockAlpha(GameObject rock, float alpha)
    {
        MeshRenderer[] renderers = rock.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            if (mr.material != null)
            {
                Color col = mr.material.color;
                col.a = alpha;
                mr.material.color = col;
            }
        }
    }

    private void TriggerMurialRocksVisual(float dmg)
    {
        if (bossTransform == null)
        {
            ApplyBossDamage(dmg);
            return;
        }

        Vector3 bossPos = bossTransform.position;
        float groundY = playerTransform != null ? playerTransform.position.y : bossPos.y;
        bossPos.y = groundY + 8.0f; // Smash much higher above map floor

        // Spawn left rock (Start fully transparent, size 800)
        GameObject leftRock = CreateRockObject("MurialRock_Left");
        leftRock.transform.position = bossPos + new Vector3(-35f, 8f, 0f);
        leftRock.transform.localScale = new Vector3(800f, 800f, 800f);
        PrepareRockMaterial(leftRock);

        // Spawn right rock (Start fully transparent, size 800)
        GameObject rightRock = CreateRockObject("MurialRock_Right");
        rightRock.transform.position = bossPos + new Vector3(35f, 8f, 0f);
        rightRock.transform.localScale = new Vector3(800f, 800f, 800f);
        PrepareRockMaterial(rightRock);

        // Animate movement to smash
        StartCoroutine(SmashRocksRoutine(leftRock, rightRock, bossPos, dmg));
    }

    private IEnumerator SmashRocksRoutine(GameObject left, GameObject right, Vector3 targetPos, float dmg)
    {
        float elapsed = 0f;
        float duration = 1.6f; // Visual fade-in + smash movement
        Vector3 startLeft = left.transform.position;
        Vector3 startRight = right.transform.position;

        BossFightCamera cam = FindFirstObjectByTypeAll<BossFightCamera>();
        if (cam != null) cam.shakeOffset = Vector3.zero;

        // Play looping earthquake sound from Resources
        AudioClip clip = Resources.Load<AudioClip>("WorldFallApart");
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.volume = 0.75f;
            audioSource.Play();
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. Fade-in alpha of the rocks (0 to 1)
            float currentAlpha = Mathf.Clamp01(t * 1.5f);
            if (left != null) SetRockAlpha(left, currentAlpha);
            if (right != null) SetRockAlpha(right, currentAlpha);

            // 2. Play continuous earthquake camera shake (starts when they appear, stops when they smash)
            if (cam != null)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * 0.9f;
                cam.shakeOffset = randomOffset;
            }

            // 3. Move rocks inward to smash
            float moveT = Mathf.Clamp01((t - 0.2f) / 0.8f); // Start moving after 20% delay
            if (moveT > 0f)
            {
                float easeInT = moveT * moveT;
                if (left != null) left.transform.position = Vector3.Lerp(startLeft, targetPos, easeInT);
                if (right != null) right.transform.position = Vector3.Lerp(startRight, targetPos, easeInT);
            }

            yield return null;
        }

        // Stop earthquake sound
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Restore original camera offset
        if (cam != null)
        {
            cam.shakeOffset = Vector3.zero;
        }

        // Visual collision / explosion sphere (Much larger!)
        GameObject explode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explode.transform.position = targetPos;
        explode.transform.localScale = Vector3.one * 20f;
        Destroy(explode.GetComponent<Collider>());
        Material expMat = new Material(Shader.Find("Sprites/Default"));
        expMat.color = new Color(0.7f, 0.45f, 0.1f, 0.6f);
        explode.GetComponent<MeshRenderer>().material = expMat;
        explode.AddComponent<SelfDestructScaler>();
        Destroy(explode, 0.5f);

        // Shake Camera with heavy impact shake at the end
        if (cam != null)
        {
            StartCoroutine(ImpactShakeRoutine(cam));
        }

        // Apply Damage
        ApplyBossDamage(dmg);

        // Destroy rocks
        Destroy(left);
        Destroy(right);
    }

    private IEnumerator ImpactShakeRoutine(BossFightCamera cam)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        float magnitude = 2.5f; // Heavy landing impact

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentMag = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * currentMag;
            cam.shakeOffset = randomOffset;
            yield return null;
        }

        cam.shakeOffset = Vector3.zero;
    }

    // ==========================================
    // FEANOR'S OSU! MINIGAME
    // ==========================================

    private void StartFeanorMinigame()
    {
        isMinigameActive = true;
        currentMinigameChar = 4;
        minigameTimer = 10f;
        osuHitCount = 0;

        // Lock player movement
        if (Nemuri.Player.PlayerMovement.Instance != null)
        {
            Nemuri.Player.PlayerMovement.Instance.SetCanMove(false);
        }

        // Enable Cursor for aim game
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        CreateOsuPanel();
        SpawnOsuTarget();
    }

    private void UpdateFeanorMinigame()
    {
        minigameTimer -= Time.deltaTime;

        if (minigameTimer <= 0f)
        {
            EndFeanorMinigame();
        }
    }

    private Sprite circleSprite;

    private Sprite GetOrCreateCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        int radius = 64;
        int size = radius * 2;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        float r2 = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float dist2 = dx * dx + dy * dy;

                if (dist2 <= r2)
                {
                    float dist = Mathf.Sqrt(dist2);
                    float alpha = Mathf.Clamp01(radius - dist); // Soft anti-aliased edge
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }
        texture.SetPixels(cols);
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    public void SpawnFloatingFeedback(Vector2 pos, string text, Color color)
    {
        if (osuPanel == null) return;

        GameObject feedbackGo = new GameObject("OsuFeedbackText");
        feedbackGo.transform.SetParent(osuPanel.transform, false);
        
        RectTransform rect = feedbackGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250f, 50f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = pos + new Vector2(0f, 40f);

        Text txt = feedbackGo.AddComponent<Text>();
        txt.text = text;
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 28;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        
        Outline outline = feedbackGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        StartCoroutine(FloatFeedbackRoutine(feedbackGo, rect, txt));
    }

    private IEnumerator FloatFeedbackRoutine(GameObject go, RectTransform rect, Text txt)
    {
        float elapsed = 0f;
        float duration = 0.8f;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 60f);

        while (elapsed < duration)
        {
            if (go == null || rect == null || txt == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            
            Color col = txt.color;
            col.a = Mathf.Lerp(1f, 0f, t);
            txt.color = col;

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private void SpawnOsuTarget()
    {
        if (osuPanel == null) return;

        // If a target exists, destroy it
        if (currentOsuTarget != null) Destroy(currentOsuTarget);

        // Create Button
        GameObject targetGo = new GameObject("OsuTargetButton");
        targetGo.transform.SetParent(osuPanel.transform, false);
        currentOsuTarget = targetGo;

        RectTransform rect = targetGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50f, 50f);
        rect.localScale = Vector3.one;
        
        // Random position relative to center of screen
        rect.anchoredPosition = new Vector2(Random.Range(-250f, 250f), Random.Range(-150f, 150f));

        Image img = targetGo.AddComponent<Image>();
        img.sprite = GetOrCreateCircleSprite(); // Use circular sprite!
        img.color = new Color(0.2f, 0.6f, 1f, 0.8f); // Blue aim circle
        img.raycastTarget = true; // Button itself must be clickable!

        // Add visual ring
        GameObject ring = new GameObject("Ring");
        ring.transform.SetParent(targetGo.transform, false);
        RectTransform ringRect = ring.AddComponent<RectTransform>();
        ringRect.sizeDelta = new Vector2(120f, 120f);
        ringRect.localScale = Vector3.one;
        ringRect.anchoredPosition = Vector2.zero;
        Image ringImg = ring.AddComponent<Image>();
        ringImg.sprite = GetOrCreateCircleSprite(); // Use circular sprite!
        ringImg.color = new Color(0.1f, 0.8f, 1f, 0.4f);
        ringImg.raycastTarget = false; // MUST be false so it does not block mouse clicks on the button!
        
        OsuRingShrinker shrinker = ring.AddComponent<OsuRingShrinker>(); // Shrinks ring to indicate perfect time

        Button btn = targetGo.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            if (shrinker != null)
            {
                float el = shrinker.GetElapsed();
                string rating = "";
                Color ratingColor = Color.white;
                float diff = Mathf.Lerp(120f, 50f, el / 1.0f) - 50f;

                if (diff <= 8f)
                {
                    rating = "PERFECT!";
                    ratingColor = new Color(1f, 0.85f, 0f); // Gold
                    osuHitCount += 2;
                }
                else if (diff <= 20f)
                {
                    rating = "GREAT!";
                    ratingColor = new Color(0.1f, 1f, 0.4f); // Green
                    osuHitCount++;
                }
                else if (diff <= 45f)
                {
                    rating = "GOOD";
                    ratingColor = new Color(0.1f, 0.7f, 1f); // Blue
                    osuHitCount++;
                }
                else
                {
                    rating = "BAD";
                    ratingColor = Color.gray;
                }

                SpawnFloatingFeedback(rect.anchoredPosition, rating, ratingColor);
            }
            SpawnOsuTarget(); // Clicked! Spawn next
        });
    }

    public void OnOsuTargetExpired()
    {
        if (isMinigameActive && currentMinigameChar == 4)
        {
            SpawnOsuTarget();
        }
    }

    private void EndFeanorMinigame()
    {
        isMinigameActive = false;
        currentMinigameChar = -1;

        if (currentOsuTarget != null) Destroy(currentOsuTarget);
        if (osuPanel != null) Destroy(osuPanel);

        // Restore player movement
        if (Nemuri.Player.PlayerMovement.Instance != null)
        {
            Nemuri.Player.PlayerMovement.Instance.SetCanMove(true);
        }

        // Do not disable the cursor (keep it visible)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Deal damage (max 5 base damage without Kael boost, boosted by Kael multiplier)
        float rawDamage = osuHitCount * 1.0f;
        float baseDamage = Mathf.Min(5f, rawDamage);
        float finalDamage = baseDamage * damageMultiplier;
        Debug.Log("[BossFightManager] Feanor Osu! Minigame ended. Clicks: " + osuHitCount + ". Base Dmg: " + baseDamage + ", Final Dmg: " + finalDamage);

        // Trigger dynamic visual effect (Blue Wave to head)
        TriggerFeanorWaveVisual(finalDamage);

        cooldownTimers[4] = 30f; // 30s cooldown
    }

    private void TriggerFeanorWaveVisual(float dmg)
    {
        if (playerTransform == null || bossTransform == null)
        {
            ApplyBossDamage(dmg);
            return;
        }

        float groundY = playerTransform.position.y;
        Vector3 startPos = playerTransform.position;
        startPos.y = groundY + 1.5f; // Eye level of player

        Vector3 targetPos = bossTransform.position;
        targetPos.y = groundY + 4.5f; // Head/Brain level of boss

        // Spawn wave sphere
        GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wave.name = "FeanorWave";
        wave.transform.position = startPos;
        wave.transform.localScale = Vector3.one * 1.5f;
        Destroy(wave.GetComponent<Collider>());

        Material waveMat = new Material(Shader.Find("Sprites/Default"));
        waveMat.color = new Color(0.1f, 0.6f, 1f, 0.7f); // Blue energy
        wave.GetComponent<MeshRenderer>().material = waveMat;

        StartCoroutine(FlyWaveRoutine(wave, startPos, targetPos, dmg));
    }

    private IEnumerator FlyWaveRoutine(GameObject wave, Vector3 start, Vector3 end, float dmg)
    {
        float elapsed = 0f;
        float duration = 1.2f; // Flight duration
        bool hasPaused = false;
        float upwardOffset = 10.0f; // 10.0 units upward offset

        MeshRenderer mr = wave != null ? wave.GetComponent<MeshRenderer>() : null;
        Material waveMat = mr != null ? mr.material : null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smoothstep easing for speed transition
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Phase 1: Moving from player origin to 10 units above the head (first 50% of timeline)
            if (smoothT < 0.5f)
            {
                if (wave != null)
                {
                    // Target position dynamically tracks above player's head as they move
                    Vector3 targetPos = playerTransform != null ? playerTransform.position + Vector3.up * upwardOffset : start + Vector3.up * upwardOffset;
                    wave.transform.position = Vector3.Lerp(start, targetPos, smoothT * 2f);
                    
                    float scale = Mathf.Lerp(1.5f, 4.0f, smoothT * 2f);
                    wave.transform.localScale = Vector3.one * scale;
                }
            }
            // Phase 2: Pause halfway for 2 seconds to charge and glow (tracks player head position every frame)
            else if (smoothT >= 0.5f && !hasPaused)
            {
                hasPaused = true;
                float pauseTimer = 0f;

                while (pauseTimer < 2.0f)
                {
                    pauseTimer += Time.deltaTime;
                    if (wave != null)
                    {
                        // Lock position 10.0 units above Feanor (active player) every single frame
                        Vector3 targetPausePos = playerTransform != null ? playerTransform.position + Vector3.up * upwardOffset : wave.transform.position;
                        wave.transform.position = targetPausePos;

                        // Pulsate scale to make it look like charging energy
                        float glowPulse = Mathf.Sin(pauseTimer * Mathf.PI * 6f) * 1.5f;
                        float currentScale = 4.0f + glowPulse;
                        wave.transform.localScale = Vector3.one * currentScale;

                        if (waveMat != null)
                        {
                            // Fade/blend to a bright white-blue glow
                            waveMat.color = Color.Lerp(new Color(0.1f, 0.6f, 1f, 0.7f), new Color(0.7f, 0.9f, 1f, 0.95f), (Mathf.Sin(pauseTimer * Mathf.PI * 4f) + 1f) / 2f);
                        }
                    }
                    yield return null;
                }

                // Update start position to release from the current head position
                if (wave != null)
                {
                    start = wave.transform.position;
                }
            }
            // Phase 3: Released from player's head and flies towards the boss brain
            else
            {
                if (wave != null)
                {
                    float finalT = (smoothT - 0.5f) * 2f;
                    Vector3 currentPos = Vector3.Lerp(start, end, finalT);
                    
                    // Spiral wave motion
                    Vector3 direction = (end - start).normalized;
                    Vector3 upDir = Vector3.up;
                    Vector3 sideDir = Vector3.Cross(direction, upDir).normalized;

                    currentPos += upDir * Mathf.Sin(finalT * Mathf.PI * 4f) * 0.8f;
                    currentPos += sideDir * Mathf.Cos(finalT * Mathf.PI * 4f) * 0.8f;

                    wave.transform.position = currentPos;
                    
                    // Pulsating scale to look like a breathing signal wave
                    float scale = Mathf.Lerp(4.0f, 6.0f, finalT) + Mathf.Sin(finalT * Mathf.PI * 8f) * 0.5f;
                    wave.transform.localScale = Vector3.one * scale;

                    if (waveMat != null)
                    {
                        waveMat.color = new Color(0.1f, 0.6f, 1f, 0.7f);
                    }
                }
            }
            yield return null;
        }

        // Strike impact
        if (wave != null)
        {
            GameObject strike = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            strike.transform.position = end;
            strike.transform.localScale = Vector3.one * 3f;
            Destroy(strike.GetComponent<Collider>());
            Material strMat = new Material(Shader.Find("Sprites/Default"));
            strMat.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            strike.GetComponent<MeshRenderer>().material = strMat;
            strike.AddComponent<SelfDestructScaler>();
            Destroy(strike, 0.4f);
        }

        ApplyBossDamage(dmg);
        Destroy(wave);
    }

    // ==========================================
    // UTILITIES & HELPER FUNCTIONS
    // ==========================================

    private void ApplyBossDamage(float amount)
    {
        BossFightTester tester = FindFirstObjectByTypeAll<BossFightTester>();
        if (tester != null)
        {
            tester.TakeDamage(amount);
        }
    }

    private void CinemachineShakeCamera()
    {
        // Simple shake calculation using FixedWorldOffsetCamera displacement or general offset
        FixedWorldOffsetCamera cam = FindFirstObjectByTypeAll<FixedWorldOffsetCamera>();
        if (cam != null)
        {
            cam.StartCoroutine(CameraShakeRoutine(cam));
        }
    }

    private IEnumerator CameraShakeRoutine(FixedWorldOffsetCamera cam)
    {
        Vector3 originalOffset = cam.worldOffset;
        float elapsed = 0f;
        float duration = 0.4f;
        float magnitude = 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * magnitude;
            cam.worldOffset = originalOffset + randomOffset;
            yield return null;
        }

        cam.worldOffset = originalOffset;
    }

    // ==========================================
    // UI LAYOUT CREATIONS
    // ==========================================

    private void CreateHUDUI()
    {
        // 1. Setup Canvas
        GameObject canvasGo = new GameObject("BossFightHUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. Setup HUD text (bottom left corner)
        GameObject textGo = new GameObject("AbilityHUDText");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rect = textGo.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(40f, 120f); // Positioned above player HP
        rect.sizeDelta = new Vector2(450f, 90f);

        hudText = textGo.AddComponent<Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudText.font == null) hudText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        hudText.fontSize = 14;
        hudText.alignment = TextAnchor.MiddleLeft;
        hudText.color = Color.white;

        // Shadow
        Shadow textShadow = textGo.AddComponent<Shadow>();
        textShadow.effectColor = Color.black;
        textShadow.effectDistance = new Vector2(1f, -1f);
    }

    private void UpdateHUDText()
    {
        if (hudText == null || Nemuri.Core.CharacterSwapManager.Instance == null) return;

        int activeIdx = Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex;
        string activeCharName = Nemuri.Core.CharacterSwapManager.Instance.GetActiveCharacterName().ToUpper();

        // Construct dynamic text depending on active character
        string lines = "<b>BOSS FIGHT HUD</b>\n";
        lines += "KAEL DAMAGE BOOST: +" + ((damageMultiplier - 1f) * 100f).ToString("F0") + "% (" + orbsCollected + " Orbs Collected)\n";

        // Show ability cooldown status
        lines += "ACTIVE CHARACTER: " + activeCharName + "\n";
        if (activeIdx == 0) // Kael
        {
            lines += "PASSIVE: ONLY Kael can see and collect glowing Yellow Orbs.\nABILITY: Cannot attack.";
        }
        else if (activeIdx == 1) // Rona
        {
            lines += "PASSIVE: Moves fast and jumps super high with [SPACE].\nABILITY: Dodge boss attacks.";
        }
        else if (activeIdx == 2) // Murial
        {
            string cd = cooldownTimers[2] > 0f ? "COOLDOWN: " + cooldownTimers[2].ToString("F1") + "s" : "READY";
            lines += "ABILITY: [L] MASH ATTACK (" + cd + ")\nSmash two massive rocks into the boss.";
        }
        else if (activeIdx == 3) // Keiko
        {
            string cd = cooldownTimers[3] > 0f ? "COOLDOWN: " + cooldownTimers[3].ToString("F1") + "s" : "READY";
            lines += "ABILITY: [L] HEAL +10 HP (" + cd + ")";
        }
        else if (activeIdx == 4) // Feanor
        {
            string cd = cooldownTimers[4] > 0f ? "COOLDOWN: " + cooldownTimers[4].ToString("F1") + "s" : "READY";
            lines += "ABILITY: [L] AIM ATTACK (" + cd + ")\nHit targets with blue signal wave.";
        }

        hudText.text = lines;
    }

    private void CreateMashPanel()
    {
        if (canvas == null) return;

        // Container Panel
        mashPanel = new GameObject("MurialMashPanel");
        mashPanel.transform.SetParent(canvas.transform, false);
        RectTransform rect = mashPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta = new Vector2(350f, 80f);
        rect.localScale = Vector3.one;

        Image bg = mashPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Add instructional text
        GameObject textGo = new GameObject("Instructions");
        textGo.transform.SetParent(mashPanel.transform, false);
        RectTransform txtRect = textGo.AddComponent<RectTransform>();
        txtRect.localScale = Vector3.one;
        txtRect.anchorMin = new Vector2(0f, 1f);
        txtRect.anchorMax = new Vector2(1f, 1f);
        txtRect.pivot = new Vector2(0.5f, 1f);
        txtRect.anchoredPosition = new Vector2(0f, -5f);
        txtRect.sizeDelta = new Vector2(0f, 30f);

        Text txt = textGo.AddComponent<Text>();
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        txt.text = "MASH [L] KEY TO ATTACK!";
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        // Progress bar background (dark red)
        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(mashPanel.transform, false);
        RectTransform barBgRect = barBg.AddComponent<RectTransform>();
        barBgRect.localScale = Vector3.one;
        barBgRect.anchorMin = new Vector2(0.05f, 0.15f);
        barBgRect.anchorMax = new Vector2(0.95f, 0.45f);
        barBgRect.pivot = new Vector2(0.5f, 0.5f);
        barBgRect.anchoredPosition = Vector2.zero;
        barBgRect.sizeDelta = Vector2.zero;

        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.3f, 0.05f, 0.05f, 1f);

        // Progress bar fill (orange/red)
        GameObject barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        mashFillRect = barFill.AddComponent<RectTransform>();
        mashFillRect.localScale = Vector3.one;
        mashFillRect.anchorMin = Vector2.zero;
        mashFillRect.anchorMax = new Vector2(0f, 1f); // Starts at 0%
        mashFillRect.pivot = new Vector2(0f, 0.5f);
        mashFillRect.anchoredPosition = Vector2.zero;
        mashFillRect.sizeDelta = Vector2.zero;

        Image barFillImg = barFill.AddComponent<Image>();
        barFillImg.color = new Color(0.9f, 0.3f, 0.1f, 1f);
    }

    private void CreateOsuPanel()
    {
        if (canvas == null) return;

        // Container Panel (takes larger center space)
        osuPanel = new GameObject("FeanorOsuPanel");
        osuPanel.transform.SetParent(canvas.transform, false);
        RectTransform rect = osuPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600f, 400f);
        rect.localScale = Vector3.one;

        Image bg = osuPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.7f); // Transparent dark blue

        // Add instructional text
        GameObject textGo = new GameObject("Instructions");
        textGo.transform.SetParent(osuPanel.transform, false);
        RectTransform txtRect = textGo.AddComponent<RectTransform>();
        txtRect.localScale = Vector3.one;
        txtRect.anchorMin = new Vector2(0f, 1f);
        txtRect.anchorMax = new Vector2(1f, 1f);
        txtRect.pivot = new Vector2(0.5f, 1f);
        txtRect.anchoredPosition = new Vector2(0f, -10f);
        txtRect.sizeDelta = new Vector2(0f, 30f);

        Text txt = textGo.AddComponent<Text>();
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        txt.text = "CLICK TARGETS WITH MOUSE!";
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.cyan;
    }

    private static T FindFirstObjectByTypeAll<T>() where T : Component
    {
        T[] objs = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in objs)
        {
            if (obj.gameObject.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }
}

// Helper animators
public class HealRingAnimator : MonoBehaviour
{
    private float elapsed = 0f;
    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.localScale = Vector3.Lerp(new Vector3(3f, 0.1f, 3f), new Vector3(10f, 0.1f, 10f), elapsed / 0.8f);
    }
}

public class OsuRingShrinker : MonoBehaviour
{
    private RectTransform rect;
    private float elapsed = 0f;

    public float GetElapsed()
    {
        return elapsed;
    }

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (rect != null)
        {
            float scale = Mathf.Lerp(120f, 50f, elapsed / 1.0f);
            rect.sizeDelta = new Vector2(scale, scale);
        }
        if (elapsed >= 1.0f)
        {
            // Auto click/miss - notify manager to spawn next target and trigger MISS rating
            if (BossFightManager.Instance != null)
            {
                RectTransform parentRect = transform.parent != null ? transform.parent.GetComponent<RectTransform>() : null;
                if (parentRect != null)
                {
                    BossFightManager.Instance.SpawnFloatingFeedback(parentRect.anchoredPosition, "MISS!", Color.red);
                }
                BossFightManager.Instance.OnOsuTargetExpired();
            }
        }
    }
}
