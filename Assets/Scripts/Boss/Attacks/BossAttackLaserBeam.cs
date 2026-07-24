using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackLaserBeam : BossAttackModule
{
    [Header("Laser Beam Settings")]
    public float chargeTime = 2.5f;
    public float beamDuration = 2.2f;
    public float beamLength = 90.0f; // From screenshot
    public float beamWidth = 16.0f;  // From screenshot
    public float damagePerSecond = 25.0f; // From screenshot

    [SerializeField] public float eyeEmissionMultiplier = 100.0f; // From screenshot

    [Header("Height Offsets (Relative to PlaneY)")]
    public float warningYOffset = 15.0f;   // Default warning offset: planeY + 15f
    public float laserBeamYOffset = 17.0f;  // Default laser visual offset: planeY + 17f

    [Header("Audio SFX Phases")]
    [SerializeField] public AudioClip chargeSound;
    [SerializeField] public AudioClip fireSound;

    [Header("Audio Volumes (0 to 5 = 0% to 500% Volume)")]
    [SerializeField, Range(0f, 5f)] public float chargeVolume = 1.0f;
    [SerializeField, Range(0f, 5f)] public float fireVolume = 1.0f;

    // Legacy field accessors for compatibility
    public AudioClip laserChargeSound { get => chargeSound; set => chargeSound = value; }
    public AudioClip laserFireSound { get => fireSound; set => fireSound = value; }

    private Transform mapCenterTransform;
    private AudioSource audioSource;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (chargeSound == null)
            chargeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/LaserCharge.wav");
        if (fireSound == null)
            fireSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/LaserFire.wav");
    }
#endif

    private void Awake()
    {
        attackName = "Boss Attack"; // From screenshot
        cooldown = 10.0f;           // From screenshot
        castDuration = 2.5f;        // From screenshot
        damage = 20.0f;             // From screenshot

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EnsureAudioClipsLoaded();
    }

    private void EnsureAudioClipsLoaded()
    {
#if UNITY_EDITOR
        if (chargeSound == null)
            chargeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/LaserCharge.wav");
        if (fireSound == null)
            fireSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/LaserFire.wav");
#endif
        if (chargeSound == null) chargeSound = Resources.Load<AudioClip>("LaserCharge");
        if (fireSound == null) fireSound = Resources.Load<AudioClip>("LaserFire");
    }

    private void Start()
    {
        // Ensure eye parts are hidden when not attacking
        SetEyeObjectsActive(false);
    }

    private void SetEyeObjectsActive(bool active)
    {
        if (transform == null) return;
        Transform eyeL = FindChildRecursive(transform, "LaserEyeL");
        Transform eyeR = FindChildRecursive(transform, "LaserEyeR");
        if (eyeL != null) eyeL.gameObject.SetActive(active);
        if (eyeR != null) eyeR.gameObject.SetActive(active);
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
        Debug.Log("[BossAttack] Executing Laser Beam (Cero)!");

        if (boss == null || targetPlayer == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 1. Reveal LaserEyeL and LaserEyeR specifically for the attack state
        SetEyeObjectsActive(true);

        Vector3 bossPos = boss.position;
        Vector3 targetPos = targetPlayer.position;
        Vector3 fireDirection = (targetPos - bossPos);
        fireDirection.y = 0f;
        if (fireDirection == Vector3.zero) fireDirection = boss.forward;
        fireDirection.Normalize();

        float planeY = GetPlaneY(bossPos);

        // Locate eye materials from LaserEyeL and LaserEyeR
        List<Material> eyeMaterials = FindLaserEyeMaterials(boss);

        // Spawn ground laser path indicator aligned to planeY + warningYOffset (planeY + 15f)
        GameObject groundPathIndicator = CreateGroundPathIndicator(bossPos, planeY + warningYOffset, fireDirection, beamLength, beamWidth);

        // 2. Charging Phase: Play LaserCharge SFX + Step-by-step smooth ramping of emission
        if (laserChargeSound != null && audioSource != null)
        {
            PlaySoundAmplified(audioSource, laserChargeSound, chargeVolume);
        }

        float elapsed = 0f;
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeTime;
            
            // Smooth step curve (easeInQuad) for dramatic build-up
            float smoothT = t * t; 
            float currentEmission = Mathf.Lerp(0f, eyeEmissionMultiplier, smoothT);

            // Bright white-red HDR color blend for intense shine/bloom
            Color hdrGlowColor = Color.Lerp(new Color(1.0f, 0.2f, 0.2f), new Color(1.0f, 0.9f, 0.8f), t) * currentEmission;

            foreach (var mat in eyeMaterials)
            {
                if (mat != null)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", hdrGlowColor);
                }
            }

            if (groundPathIndicator != null)
            {
                var ren = groundPathIndicator.GetComponent<Renderer>();
                if (ren != null && ren.material != null)
                {
                    float alpha = Mathf.Lerp(0.3f, 0.85f, t) + Mathf.Sin(Time.time * 16f) * 0.12f;
                    ren.material.SetColor("_BaseColor", new Color(1.0f, 0.05f, 0.05f, Mathf.Clamp01(alpha)));
                }
            }

            yield return null;
        }

        if (groundPathIndicator != null) Destroy(groundPathIndicator);

        // 3. Firing Phase (2.2s): Stop charging audio, play LaserFire SFX + Activate red screen overlay + camera shake
        if (audioSource != null)
        {
            audioSource.Stop(); // Stop charging sound immediately!
            if (laserFireSound != null)
            {
                PlaySoundAmplified(audioSource, laserFireSound, fireVolume);
            }
        }

        Vector3 beamOrigin = new Vector3(bossPos.x, planeY + laserBeamYOffset, bossPos.z);
        GameObject laserBeamObj = CreateLaserBeamVisual(beamOrigin, fireDirection, beamLength, beamWidth);

        if (LaserBeamRedScreenUI.Instance != null)
        {
            LaserBeamRedScreenUI.Instance.SetRedScreenActive(true);
        }

        BossFightCamera cam = FindBossCamera();

        float fireElapsed = 0f;
        float tickInterval = 0.2f;
        float nextTickTime = 0f;

        while (fireElapsed < beamDuration)
        {
            fireElapsed += Time.deltaTime;

            if (cam != null)
            {
                cam.shakeOffset = new Vector3(
                    Random.Range(-0.85f, 0.85f),
                    Random.Range(-0.85f, 0.85f),
                    Random.Range(-0.85f, 0.85f)
                );
            }

            if (fireElapsed >= nextTickTime && targetPlayer != null)
            {
                nextTickTime = fireElapsed + tickInterval;
                if (IsPlayerInBeamPath(beamOrigin, fireDirection, beamLength, beamWidth, targetPlayer.position))
                {
                    var ph = targetPlayer.GetComponent<PlayerHealth>();
                    if (ph == null && targetPlayer.parent != null) ph = targetPlayer.parent.GetComponent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(damagePerSecond * tickInterval);
                    }
                }
            }

            yield return null;
        }

        if (cam != null) cam.shakeOffset = Vector3.zero;

        if (LaserBeamRedScreenUI.Instance != null)
        {
            LaserBeamRedScreenUI.Instance.SetRedScreenActive(false);
        }

        if (laserBeamObj != null) Destroy(laserBeamObj);

        // Reset emission and hide eye objects after attack completes
        foreach (var mat in eyeMaterials)
        {
            if (mat != null)
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }

        SetEyeObjectsActive(false);

        if (audioSource != null) audioSource.Stop();

        onComplete?.Invoke();
    }

    private BossFightCamera FindBossCamera()
    {
        BossFightCamera[] cams = Resources.FindObjectsOfTypeAll<BossFightCamera>();
        foreach (var c in cams)
        {
            if (c.gameObject.scene.isLoaded) return c;
        }
        return null;
    }

    private List<Material> FindLaserEyeMaterials(Transform boss)
    {
        List<Material> results = new List<Material>();
        if (boss == null) return results;

        Transform eyeL = FindChildRecursive(boss, "LaserEyeL");
        Transform eyeR = FindChildRecursive(boss, "LaserEyeR");

        if (eyeL != null)
        {
            Renderer renL = eyeL.GetComponent<Renderer>();
            if (renL != null) results.AddRange(renL.materials);
        }

        if (eyeR != null)
        {
            Renderer renR = eyeR.GetComponent<Renderer>();
            if (renR != null) results.AddRange(renR.materials);
        }

        return results;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private GameObject CreateGroundPathIndicator(Vector3 origin, float yHeight, Vector3 dir, float length, float width)
    {
        GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
        path.name = "LaserBeam_GroundPathIndicator";
        
        Vector3 center = origin + dir * (length * 0.5f);
        center.y = yHeight;

        path.transform.position = center;
        path.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        path.transform.localScale = new Vector3(width, 0.02f, length);
        Destroy(path.GetComponent<Collider>());

        Renderer ren = path.GetComponent<Renderer>();
        if (ren != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(1.0f, 0.05f, 0.05f, 0.5f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1.0f, 0.05f, 0.05f) * 4f);
            ren.sharedMaterial = mat;
        }
        return path;
    }

    private GameObject CreateLaserBeamVisual(Vector3 beamOrigin, Vector3 dir, float length, float width)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "LaserBeam_EnergyVisual";
        
        Vector3 center = beamOrigin + dir * (length * 0.5f);
        beam.transform.position = center;
        beam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        beam.transform.localScale = new Vector3(width, length * 0.5f, width);
        Destroy(beam.GetComponent<Collider>());

        Renderer ren = beam.GetComponent<Renderer>();
        if (ren != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(1.0f, 0.15f, 0.15f, 0.95f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1.0f, 0.05f, 0.05f) * 12f);
            ren.sharedMaterial = mat;
        }

        return beam;
    }

    private bool IsPlayerInBeamPath(Vector3 origin, Vector3 dir, float length, float width, Vector3 playerPos)
    {
        Vector3 playerOffset = playerPos - origin;
        float distanceAlongBeam = Vector3.Dot(playerOffset, dir);
        if (distanceAlongBeam < 0f || distanceAlongBeam > length) return false;

        Vector3 closestPoint = origin + dir * distanceAlongBeam;
        float perpendicularDist = Vector3.Distance(new Vector3(playerPos.x, origin.y, playerPos.z), new Vector3(closestPoint.x, origin.y, closestPoint.z));
        return perpendicularDist <= (width * 0.5f + 0.7f);
    }
}
