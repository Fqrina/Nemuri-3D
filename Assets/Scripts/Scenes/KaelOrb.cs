using UnityEngine;

public class KaelOrb : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Light orbLight;
    private bool collected = false;

    private void Awake()
    {
        // Programmatically setup mesh, collider, materials, and lights to ensure zero manual setup
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshRenderer = sphere.GetComponent<MeshRenderer>();
            
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            
            gameObject.AddComponent<MeshRenderer>();
            meshRenderer = GetComponent<MeshRenderer>();
            Destroy(sphere);
        }

        // Apply a glowing green color
        Material orbMaterial = new Material(Shader.Find("Sprites/Default"));
        orbMaterial.color = new Color(1f, 0.85f, 0.1f, 0.8f); // Glowing Yellow
        meshRenderer.material = orbMaterial;

        // Scale to a nice size
        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

        // Add a trigger collider
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.0f;

        // Add a yellow point light
        orbLight = gameObject.AddComponent<Light>();
        orbLight.type = LightType.Point;
        orbLight.color = Color.yellow;
        orbLight.range = 8f;
        orbLight.intensity = 3f;
    }

    private void Update()
    {
        if (collected) return;

        // Rotate/float effect to look animated
        transform.Rotate(Vector3.up * 45f * Time.deltaTime);
        transform.position += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.002f;

        // Visibility check: Only visible and obtainable if Kael (index 0) is active
        bool isKael = (Nemuri.Core.CharacterSwapManager.Instance != null && 
                       Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex == 0);

        if (meshRenderer.enabled != isKael)
        {
            meshRenderer.enabled = isKael;
            if (orbLight != null) orbLight.enabled = isKael;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Only collect if colliding with player and current active character is Kael
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player") || other.gameObject.name.Contains("chara"))
        {
            bool isKael = (Nemuri.Core.CharacterSwapManager.Instance != null && 
                           Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex == 0);

            if (isKael)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        collected = true;
        
        // Notify Manager to increase multiplier by 15%
        if (BossFightManager.Instance != null)
        {
            BossFightManager.Instance.CollectOrb();
        }

        // Spawn a yellow particle ring/flash effect
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = transform.position;
        flash.transform.localScale = Vector3.zero;
        Destroy(flash.GetComponent<Collider>());
        
        Material flashMat = new Material(Shader.Find("Sprites/Default"));
        flashMat.color = new Color(1f, 0.85f, 0.1f, 0.5f); // Glowing Yellow after-effect
        flash.GetComponent<MeshRenderer>().material = flashMat;
        
        // Simple scale up and destroy animation for the particle
        Destroy(flash, 0.4f);
        flash.AddComponent<SelfDestructScaler>();

        Destroy(gameObject);
    }
}

public class SelfDestructScaler : MonoBehaviour
{
    private float elapsed = 0f;
    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 3f, elapsed / 0.4f);
    }
}
