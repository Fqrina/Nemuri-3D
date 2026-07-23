using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackIntrusiveThoughts : BossAttackModule
{
    [Header("Intrusive Thoughts Settings")]
    public int projectileCount = 10; // 10 spots as requested
    public float spawnHeight = 18.0f;
    public float chargeDuration = 2.0f;
    public float fallSpeed = 40.0f;
    public float indicatorRadius = 5.5f; // Made area of attack bigger
    public float minInnerRadius = 10.0f;
    public float maxOuterRadius = 20.0f; // Adjusted to 20 meters as requested
    public float groundYOffset = 0.6f;    // Reverted back to planeY + 0.6f as requested

    [Header("Audio SFX Phases")]
    [SerializeField] public AudioClip whisperSound;
    [SerializeField] public AudioClip chargeSound;
    [SerializeField] public AudioClip fallSound;

    [Header("Audio Volumes (0 to 5 = 0% to 500% Volume)")]
    [SerializeField, Range(0f, 5f)] public float whisperVolume = 1.0f;
    [SerializeField, Range(0f, 5f)] public float chargeVolume = 1.0f;
    [SerializeField, Range(0f, 5f)] public float fallVolume = 1.0f;

    // Legacy field accessors for compatibility
    public AudioClip intrusiveWhisperSound { get => whisperSound; set => whisperSound = value; }
    public AudioClip intrusiveChargeSound { get => chargeSound; set => chargeSound = value; }
    public AudioClip intrusiveFallSound { get => fallSound; set => fallSound = value; }

    private Transform mapCenterTransform;
    private AudioSource audioSource;
    private AudioSource whisperAudioSource;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (whisperSound == null)
            whisperSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/IntrusiveWhisper.wav");
        if (chargeSound == null)
            chargeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/IntrusiveCharge.wav");
        if (fallSound == null)
            fallSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/IntrusiveFall.wav");
    }
#endif

    private void Awake()
    {
        attackName = "Intrusive Thoughts";
        cooldown = 12.0f;
        castDuration = 3.5f;
        damage = 25.0f;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        whisperAudioSource = gameObject.AddComponent<AudioSource>();
        whisperAudioSource.playOnAwake = false;
        whisperAudioSource.loop = true;

        EnsureAudioClipsLoaded();
    }

    private void EnsureAudioClipsLoaded()
    {
#if UNITY_EDITOR
        if (whisperSound == null)
            whisperSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/IntrusiveWhisper.wav");
        if (chargeSound == null)
            chargeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/IntrusiveCharge.wav");
        if (fallSound == null)
            fallSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/IntrusiveFall.wav");
#endif
        if (whisperSound == null) whisperSound = Resources.Load<AudioClip>("IntrusiveWhisper");
        if (chargeSound == null) chargeSound = Resources.Load<AudioClip>("IntrusiveCharge");
        if (fallSound == null) fallSound = Resources.Load<AudioClip>("IntrusiveFall");
    }

    private Transform GetMapCenterTransform(Transform boss)
    {
        if (mapCenterTransform != null) return mapCenterTransform;

        GameObject mapObj = GameObject.Find("AMYGDALA");
        if (mapObj == null) mapObj = GameObject.Find("AMYGDALA(Clone)");
        if (mapObj == null)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name.Contains("AMYGDALA") && go.scene.isLoaded)
                {
                    mapObj = go;
                    break;
                }
            }
        }
        if (mapObj != null) mapCenterTransform = mapObj.transform;
        return mapCenterTransform != null ? mapCenterTransform : boss;
    }

    private float GetPlaneY(Vector3 pos)
    {
        Ray ray = new Ray(new Vector3(pos.x, 300.0f, pos.z), Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 600f, ~0);
        float highestY = -999f;
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.isTrigger) continue; // Ignore triggers

            string nameLower = hit.collider.gameObject.name.ToLower();
            if (nameLower.Contains("rabbit") || nameLower.Contains("boss") || hit.collider.gameObject.CompareTag("Enemy")) continue; // Ignore boss/enemy
            if (hit.collider.gameObject.CompareTag("Player") || nameLower.Contains("player") || nameLower.Contains("chara")) continue;
            
            if (hit.point.y > highestY)
            {
                highestY = hit.point.y;
            }
        }

        if (highestY > -900f) return highestY;

        Transform mapCenter = GetMapCenterTransform(transform);
        if (mapCenter != null) return mapCenter.position.y;

        return pos.y;
    }

    private void PlaySoundAmplified(AudioSource source, AudioClip clip, float volume)
    {
        if (clip == null || source == null || volume <= 0f) return;
        int fullLayers = Mathf.FloorToInt(volume);
        float remainder = volume - fullLayers;
        for (int i = 0; i < fullLayers; i++)
        {
            source.PlayOneShot(clip, 1.0f);
        }
        if (remainder > 0.001f)
        {
            source.PlayOneShot(clip, remainder);
        }
    }

    public override IEnumerator ExecuteAttackRoutine(Transform boss, Transform targetPlayer, System.Action onComplete)
    {
        lastCastTime = Time.time;
        Debug.Log("[BossAttack] Executing Intrusive Thoughts!");

        Transform centerTransform = GetMapCenterTransform(boss);
        Vector3 centerPos = centerTransform != null ? centerTransform.position : Vector3.zero;

        List<Vector3> landingPositions = new List<Vector3>();
        List<GameObject> indicators = new List<GameObject>();
        List<GameObject> spheres = new List<GameObject>();

        // Generate target positions in donut radius (10m to 20m)
        for (int i = 0; i < projectileCount; i++)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomDist = Random.Range(minInnerRadius, maxOuterRadius);

            Vector3 offset = new Vector3(Mathf.Cos(randomAngle) * randomDist, 0f, Mathf.Sin(randomAngle) * randomDist);
            Vector3 targetLandPos = centerPos + offset;
            
            float planeY = GetPlaneY(targetLandPos);
            targetLandPos.y = planeY;
            landingPositions.Add(targetLandPos);

            // Ground circular indicator with planeY + 0.6f height offset
            GameObject indicator = CreateCircularGroundIndicator(targetLandPos, indicatorRadius);
            indicators.Add(indicator);

            // High spell sphere
            Vector3 startSpherePos = targetLandPos + Vector3.up * spawnHeight;
            GameObject sphere = CreateSpellSphere(startSpherePos);
            sphere.transform.localScale = Vector3.one * 0.4f;
            spheres.Add(sphere);
        }

        // 1. Play IntrusiveWhisper (looping for entire duration) + IntrusiveCharge
        if (intrusiveWhisperSound != null && whisperAudioSource != null)
        {
            whisperAudioSource.clip = intrusiveWhisperSound;
            whisperAudioSource.volume = Mathf.Clamp01(whisperVolume);
            whisperAudioSource.Play();
        }
        if (intrusiveChargeSound != null && audioSource != null)
        {
            PlaySoundAmplified(audioSource, intrusiveChargeSound, chargeVolume);
        }

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeDuration;
            float currentScale = Mathf.Lerp(0.5f, 4.5f, t); // Made sphere visually bigger (max scale 4.5)

            for (int i = 0; i < spheres.Count; i++)
            {
                if (spheres[i] != null)
                {
                    spheres[i].transform.localScale = Vector3.one * currentScale;

                    // Dynamically attach pink smoke particles if not already present
                    if (spheres[i].transform.childCount == 0)
                    {
                        CreatePinkSmokeParticles(spheres[i]);
                    }
                }
                if (indicators[i] != null)
                {
                    var renderer = indicators[i].GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        float alpha = Mathf.PingPong(Time.time * 6f, 0.4f) + 0.45f;
                        renderer.material.SetColor("_BaseColor", new Color(0.85f, 0.1f, 0.95f, alpha));
                    }
                }
            }
            yield return null;
        }

        // 2. Stop charging sound & play IntrusiveFall SFX when spell spheres drop
        if (audioSource != null)
        {
            audioSource.Stop(); // Stop charging sound immediately!
            if (intrusiveFallSound != null)
            {
                PlaySoundAmplified(audioSource, intrusiveFallSound, fallVolume);
            }
        }

        bool allLanded = false;
        while (!allLanded)
        {
            allLanded = true;
            for (int i = 0; i < spheres.Count; i++)
            {
                if (spheres[i] == null) continue;

                Vector3 currentPos = spheres[i].transform.position;
                Vector3 targetPos = landingPositions[i];

                if (currentPos.y > targetPos.y + 0.8f)
                {
                    allLanded = false;
                    spheres[i].transform.position = Vector3.MoveTowards(currentPos, targetPos, fallSpeed * Time.deltaTime);
                }
                else
                {
                    TriggerImpact(targetPos, targetPlayer, indicatorRadius, damage);
                    if (indicators[i] != null) Destroy(indicators[i]);
                    Destroy(spheres[i]);
                    spheres[i] = null;
                }
            }
            yield return null;
        }

        foreach (var ind in indicators)
        {
            if (ind != null) Destroy(ind);
        }

        if (whisperAudioSource != null && whisperAudioSource.isPlaying)
        {
            whisperAudioSource.Stop();
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        onComplete?.Invoke();
    }

    private GameObject CreateCircularGroundIndicator(Vector3 position, float radius)
    {
        GameObject ind = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ind.name = "IntrusiveThoughts_GroundIndicator";
        ind.transform.position = new Vector3(position.x, position.y + groundYOffset, position.z);
        ind.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        Destroy(ind.GetComponent<Collider>());

        Renderer ren = ind.GetComponent<Renderer>();
        if (ren != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.85f, 0.1f, 0.95f, 0.6f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.95f, 0.1f, 1.0f) * 3f);
            ren.sharedMaterial = mat;
        }
        return ind;
    }

    private GameObject CreateSpellSphere(Vector3 position)
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "IntrusiveThoughts_Sphere";
        s.transform.position = position;
        Destroy(s.GetComponent<Collider>());

        Renderer ren = s.GetComponent<Renderer>();
        if (ren != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.7f, 0.0f, 0.9f, 1.0f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.9f, 0.1f, 1.0f) * 5f);
            ren.sharedMaterial = mat;
        }
        return s;
    }

    private void TriggerImpact(Vector3 impactPos, Transform player, float radius, float dmg)
    {
        if (player != null)
        {
            float dist = Vector3.Distance(new Vector3(player.position.x, impactPos.y, player.position.z), impactPos);
            if (dist <= radius)
            {
                var ph = player.GetComponent<PlayerHealth>();
                if (ph == null && player.parent != null) ph = player.parent.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(dmg);
                }
            }
        }

        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fx.name = "ImpactWave";
        fx.transform.position = new Vector3(impactPos.x, impactPos.y + groundYOffset, impactPos.z);
        fx.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        Destroy(fx.GetComponent<Collider>());

        Renderer ren = fx.GetComponent<Renderer>();
        if (ren != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(1.0f, 0.2f, 0.95f, 0.8f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1.0f, 0.3f, 1.0f) * 6f);
            ren.sharedMaterial = mat;
        }

        BossAttackController.Instance.StartCoroutine(AnimateImpactShockwave(fx, radius * 2.2f));
    }

    private IEnumerator AnimateImpactShockwave(GameObject fx, float maxDiameter)
    {
        float elapsed = 0f;
        float duration = 0.4f;
        Vector3 startScale = new Vector3(0.5f, 0.05f, 0.5f);
        Vector3 targetScale = new Vector3(maxDiameter, 0.05f, maxDiameter);

        while (elapsed < duration)
        {
            if (fx == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fx.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        if (fx != null) Destroy(fx);
    }

    private void CreatePinkSmokeParticles(GameObject parent)
    {
        GameObject pObj = new GameObject("PinkSmokeParticles");
        pObj.transform.SetParent(parent.transform, false);
        pObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        
        // Configure main module
        var main = ps.main;
        main.startColor = new Color(1.0f, 0.2f, 0.7f, 0.45f); // Beautiful semi-transparent pink
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f); // Made particles smaller as requested
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        // Configure emission
        var emission = ps.emission;
        emission.rateOverTime = 30f;

        // Configure shape (sphere emission around the spell sphere)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.2f;

        // Configure color over lifetime (fades out at end)
        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.2f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.1f, 0.5f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1.0f) }
        );
        colorModule.color = new ParticleSystem.MinMaxGradient(grad);

        // Apply URP Lit particle material
        ParticleSystemRenderer psr = pObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (particleMat == null) particleMat = new Material(Shader.Find("Sprites/Default"));
            particleMat.SetColor("_BaseColor", new Color(1.0f, 0.2f, 0.7f, 0.5f));
            psr.sharedMaterial = particleMat;
        }

        ps.Play();
    }
}
