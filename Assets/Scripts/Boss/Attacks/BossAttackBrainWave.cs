using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackBrainWave : BossAttackModule
{
    [Header("Brain Wave (Wi-Fi Bullet Hell) Settings")]
    public int waveCount = 3;
    public int arcsPerWave = 6;
    public float fanAngleDegrees = 120.0f;
    public float waveSpeed = 22.0f; // Increased speed for the longer distance
    public float waveInterval = 1.1f;
    public float maxDistance = 120.0f; // Made way longer
    public float heightOffsetFromPlane = 1.0f; // Lowered offset to 1.0f as requested

    [Header("Audio SFX Phases")]
    [SerializeField] public AudioClip waveSound;

    [Header("Audio Volumes (0 to 5 = 0% to 500% Volume)")]
    [SerializeField, Range(0f, 5f)] public float waveVolume = 1.0f;

    // Legacy field accessor for compatibility
    public AudioClip brainWaveSound { get => waveSound; set => waveSound = value; }

    private static Mesh cached4TierWifiMesh;
    private Transform mapCenterTransform;
    private AudioSource audioSource;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (waveSound == null)
            waveSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BrainWave.wav");
    }
#endif

    private void Awake()
    {
        attackName = "Brain Wave (Wi-Fi Wave)";
        cooldown = 15.0f;
        castDuration = 3.5f;
        damage = 16.0f;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EnsureAudioClipsLoaded();

        if (cached4TierWifiMesh == null)
        {
            // Increased ribbonWidth and mesh overall size
            cached4TierWifiMesh = Create4TierWifiArcMesh(45f, 1.2f, 18);
        }
    }

    private void EnsureAudioClipsLoaded()
    {
#if UNITY_EDITOR
        if (waveSound == null)
            waveSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BrainWave.wav");
#endif
        if (waveSound == null) waveSound = Resources.Load<AudioClip>("BrainWave");
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
        Debug.Log("[BossAttack] Executing Brain Wave (Wi-Fi Wave)!");

        if (boss == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float planeY = GetPlaneY(boss.position);
        Vector3 spawnOrigin = new Vector3(boss.position.x, planeY + heightOffsetFromPlane, boss.position.z);

        Transform mapCenter = GetMapCenterTransform(boss);
        Vector3 mapDir = mapCenter != null ? (mapCenter.position - boss.position) : boss.forward;
        mapDir.y = 0f;
        if (mapDir.sqrMagnitude < 0.1f) mapDir = boss.forward;
        mapDir.Normalize();

        for (int w = 0; w < waveCount; w++)
        {
            if (brainWaveSound != null && audioSource != null)
            {
                PlaySoundAmplified(audioSource, brainWaveSound, waveVolume);
            }

            int gapIndex = Random.Range(1, arcsPerWave - 1);

            List<GameObject> waveArcs = new List<GameObject>();
            List<Vector3> waveDirections = new List<Vector3>();

            float startAngle = -fanAngleDegrees * 0.5f;
            float endAngle = fanAngleDegrees * 0.5f;

            for (int i = 0; i < arcsPerWave; i++)
            {
                if (i == gapIndex) continue;

                float t = (arcsPerWave > 1) ? (float)i / (arcsPerWave - 1) : 0.5f;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                Vector3 moveDir = Quaternion.Euler(0f, angle, 0f) * mapDir;

                GameObject arcObj = Create4TierWifiArcGameObject(spawnOrigin, moveDir);
                waveArcs.Add(arcObj);
                waveDirections.Add(moveDir);
            }

            BossAttackController.Instance.StartCoroutine(AnimateWaveExpansion(waveArcs, waveDirections, targetPlayer));

            yield return new WaitForSeconds(waveInterval);
        }

        onComplete?.Invoke();
    }

    private GameObject Create4TierWifiArcGameObject(Vector3 position, Vector3 direction)
    {
        GameObject arc = new GameObject("BrainWave_4TierWifiArc");
        arc.transform.position = position;
        arc.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        MeshFilter mf = arc.AddComponent<MeshFilter>();
        mf.mesh = cached4TierWifiMesh;

        MeshRenderer ren = arc.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.1f, 0.7f, 1.0f, 0.95f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1.0f) * 6f);
        ren.sharedMaterial = mat;

        return arc;
    }

    /// <summary>
    /// Procedurally builds a 4-tier Wi-Fi wave mesh (4 nested curved Wi-Fi signal arcs ⌒ ⌒ ⌒ ⌒) forming 1 multi-bar signal wave!
    /// </summary>
    private Mesh Create4TierWifiArcMesh(float arcAngleDegrees, float ribbonWidth, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "4TierWifiArcMesh";

        List<Vector3> vertsList = new List<Vector3>();
        List<Vector2> uvsList = new List<Vector2>();
        List<int> trisList = new List<int>();

        // Increased radii making the visual wave much bigger!
        float[] radii = new float[] { 3.5f, 6.0f, 8.5f, 11.0f };

        float startAngle = -arcAngleDegrees * 0.5f * Mathf.Deg2Rad;
        float endAngle = arcAngleDegrees * 0.5f * Mathf.Deg2Rad;

        foreach (float radius in radii)
        {
            int baseVertIndex = vertsList.Count;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t);

                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);

                float innerR = radius - ribbonWidth * 0.5f;
                float outerR = radius + ribbonWidth * 0.5f;

                vertsList.Add(new Vector3(sin * innerR, 0f, cos * innerR - radius));
                vertsList.Add(new Vector3(sin * outerR, 0f, cos * outerR - radius));

                uvsList.Add(new Vector2(t, 0f));
                uvsList.Add(new Vector2(t, 1f));

                if (i < segments)
                {
                    int v = baseVertIndex + i * 2;
                    trisList.Add(v);
                    trisList.Add(v + 1);
                    trisList.Add(v + 2);

                    trisList.Add(v + 1);
                    trisList.Add(v + 3);
                    trisList.Add(v + 2);
                }
            }
        }

        mesh.vertices = vertsList.ToArray();
        mesh.uv = uvsList.ToArray();
        mesh.triangles = trisList.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private IEnumerator AnimateWaveExpansion(List<GameObject> arcs, List<Vector3> directions, Transform player)
    {
        float traveled = 0f;
        HashSet<GameObject> hitArcs = new HashSet<GameObject>();

        while (traveled < maxDistance)
        {
            float step = waveSpeed * Time.deltaTime;
            traveled += step;

            for (int i = arcs.Count - 1; i >= 0; i--)
            {
                GameObject arc = arcs[i];
                if (arc == null) continue;

                arc.transform.position += directions[i] * step;

                if (player != null && !hitArcs.Contains(arc))
                {
                    Vector3 playerXZ = new Vector3(player.position.x, arc.transform.position.y, player.position.z);
                    float dist = Vector3.Distance(playerXZ, arc.transform.position);

                    float yDiff = Mathf.Abs(player.position.y - arc.transform.position.y);

                    if (dist <= 3.2f && yDiff <= 4.0f)
                    {
                        hitArcs.Add(arc);
                        var ph = player.GetComponent<PlayerHealth>();
                        if (ph == null && player.parent != null) ph = player.parent.GetComponent<PlayerHealth>();
                        if (ph != null)
                        {
                            ph.TakeDamage(damage);
                        }
                        Destroy(arc);
                        arcs[i] = null;
                    }
                }
            }

            yield return null;
        }

        foreach (var arc in arcs)
        {
            if (arc != null) Destroy(arc);
        }
    }
}
