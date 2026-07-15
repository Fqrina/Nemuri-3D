using System.Collections.Generic;
using UnityEngine;
using Nemuri.Player;

namespace Nemuri.CameraEffects
{
    public class CameraObstructionManager : MonoBehaviour
    {
        private class ObstructedObject
        {
            public GameObject gameObject;
            public Renderer[] renderers;
            public Material[][] originalMaterials;
            public float cooldownTimer;
        }

        [Header("Settings")]
        [SerializeField] private float _cameraSphereRadius = 1.8f;
        [SerializeField] private string _disappearLayerName = "CameraDisappear";
        [SerializeField] private float _fadeCooldown = 0.35f; // Prevents stuttering/flickering
        [SerializeField] private float _targetAlpha = 0.25f;  // Makes trees transparent instead of fully disappearing

        private Transform _playerTransform;
        private int _disappearLayer;
        private int _layerMask;

        private Dictionary<GameObject, ObstructedObject> _obstructedObjects = new Dictionary<GameObject, ObstructedObject>();

        private void Start()
        {
            _disappearLayer = LayerMask.NameToLayer(_disappearLayerName);
            if (_disappearLayer == -1)
            {
                Debug.LogWarning($"[CameraObstructionManager] Layer '{_disappearLayerName}' was not found in the project. Fallback to default.");
                _disappearLayer = 0;
            }
            _layerMask = 1 << _disappearLayer;
        }

        private void Update()
        {
            FindActivePlayer();

            Vector3 cameraPos = transform.position;
            float distToPlayer = _playerTransform != null ? Vector3.Distance(cameraPos, _playerTransform.position) : 999f;

            // Find all potential obstructions in a 15-unit radius around the camera
            Collider[] colliders = Physics.OverlapSphere(cameraPos, 15f, _layerMask);
            foreach (var col in colliders)
            {
                if (col == null) continue;

                Vector3 treePos = col.bounds.center;
                float distToTree = Vector3.Distance(cameraPos, treePos);

                bool shouldFade = false;

                // 1. Clipping check (inside or extremely close to the camera)
                if (distToTree < 2.0f)
                {
                    shouldFade = true;
                }
                // 2. Viewport check (only objects obstructing the center 50% of screen)
                else if (_playerTransform != null && distToTree < distToPlayer)
                {
                    Vector3 viewportPos = Camera.main.WorldToViewportPoint(treePos);
                    if (viewportPos.z > 0f)
                    {
                        // Check if projected point is within the center 50% of the screen
                        if (viewportPos.x >= 0.25f && viewportPos.x <= 0.75f &&
                            viewportPos.y >= 0.20f && viewportPos.y <= 0.80f)
                        {
                            shouldFade = true;
                        }
                    }
                }

                if (shouldFade)
                {
                    RegisterObstruction(col.gameObject);
                }
            }

            // 3. Update cooldowns and restore objects that are no longer obstructing
            List<GameObject> toRestore = new List<GameObject>();
            List<GameObject> activeKeys = new List<GameObject>(_obstructedObjects.Keys);

            foreach (var key in activeKeys)
            {
                var obs = _obstructedObjects[key];
                obs.cooldownTimer -= Time.deltaTime;
                if (obs.cooldownTimer <= 0f)
                {
                    toRestore.Add(key);
                }
            }

            foreach (var key in toRestore)
            {
                RestoreObstruction(key);
            }
        }

        private void RegisterObstruction(GameObject obj)
        {
            // Find root-most parent of the obstruction that is still on the CameraDisappear layer
            GameObject rootObj = obj;
            Transform parent = obj.transform.parent;
            while (parent != null && parent.gameObject.layer == _disappearLayer)
            {
                rootObj = parent.gameObject;
                parent = parent.parent;
            }

            if (_obstructedObjects.TryGetValue(rootObj, out ObstructedObject existing))
            {
                existing.cooldownTimer = _fadeCooldown; // Reset cooldown
                return;
            }

            // Get all renderers in this object tree
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            ObstructedObject obs = new ObstructedObject();
            obs.gameObject = rootObj;
            obs.renderers = renderers;
            obs.cooldownTimer = _fadeCooldown;
            obs.originalMaterials = new Material[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                obs.originalMaterials[i] = renderer.sharedMaterials;

                // Create instanced transparent materials
                Material[] instancedMaterials = renderer.materials;
                foreach (var mat in instancedMaterials)
                {
                    MakeMaterialTransparent(mat);
                }
                renderer.materials = instancedMaterials;
            }

            _obstructedObjects.Add(rootObj, obs);
        }

        private void RestoreObstruction(GameObject rootObj)
        {
            if (_obstructedObjects.TryGetValue(rootObj, out ObstructedObject obs))
            {
                for (int i = 0; i < obs.renderers.Length; i++)
                {
                    if (obs.renderers[i] != null)
                    {
                        // Restore original shared materials
                        obs.renderers[i].sharedMaterials = obs.originalMaterials[i];
                    }
                }
                _obstructedObjects.Remove(rootObj);
            }
        }

        private void MakeMaterialTransparent(Material mat)
        {
            if (mat == null) return;

            Color col = Color.white;
            if (mat.HasProperty("_Color"))
            {
                col = mat.GetColor("_Color");
                mat.SetColor("_Color", new Color(col.r, col.g, col.b, _targetAlpha));
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                col = mat.GetColor("_BaseColor");
                mat.SetColor("_BaseColor", new Color(col.r, col.g, col.b, _targetAlpha));
            }

            string shaderName = mat.shader.name;
            if (shaderName.Contains("Universal Render Pipeline") || shaderName.Contains("URP"))
            {
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f); // Alpha Blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                // Standard Shader fallback
                mat.SetFloat("_Mode", 3f); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }

        private void FindActivePlayer()
        {
            if (PlayerMovementChapt1.Instance != null && PlayerMovementChapt1.Instance.gameObject.activeInHierarchy)
            {
                _playerTransform = PlayerMovementChapt1.Instance.transform;
                return;
            }
            if (PlayerMovement.Instance != null && PlayerMovement.Instance.gameObject.activeInHierarchy)
            {
                _playerTransform = PlayerMovement.Instance.transform;
                return;
            }

            GameObject defaultPlayer = GameObject.FindWithTag("Player");
            if (defaultPlayer != null && defaultPlayer.activeInHierarchy)
            {
                _playerTransform = defaultPlayer.transform;
            }
        }

        private void OnDisable()
        {
            // Restore all objects immediately when component is disabled/destroyed
            List<GameObject> activeKeys = new List<GameObject>(_obstructedObjects.Keys);
            foreach (var key in activeKeys)
            {
                RestoreObstruction(key);
            }
            _obstructedObjects.Clear();
        }
    }
}
