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
            public Collider[] colliders;
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

            HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();

            // 1. OverlapSphere to capture close clipping objects around camera
            Collider[] closeColliders = Physics.OverlapSphere(cameraPos, 3.0f, _layerMask);
            foreach (var col in closeColliders)
            {
                if (col != null) uniqueRoots.Add(GetRootObstruction(col.gameObject));
            }

            // 2. SphereCast along the view line from camera to player to find direct blockers
            if (_playerTransform != null)
            {
                Vector3 targetPoint = _playerTransform.position + Vector3.up * 1.0f; // Target player torso
                Vector3 toPlayer = targetPoint - cameraPos;
                float castDist = toPlayer.magnitude;
                if (castDist > 0.1f)
                {
                    RaycastHit[] hits = Physics.SphereCastAll(cameraPos, 1.2f, toPlayer.normalized, castDist, _layerMask);
                    foreach (var hit in hits)
                    {
                        if (hit.collider != null) uniqueRoots.Add(GetRootObstruction(hit.collider.gameObject));
                    }
                }
            }

            // Process each unique root object
            foreach (var rootObj in uniqueRoots)
            {
                Vector3 rootPos = rootObj.transform.position;

                // Hardcode Y-level check: skip if below player plane
                if (_playerTransform != null && rootPos.y < _playerTransform.position.y - 0.5f)
                {
                    continue;
                }

                float distToRoot = Vector3.Distance(cameraPos, rootPos);
                bool shouldFade = false;

                // Clipping check
                if (distToRoot < 3.0f)
                {
                    shouldFade = true;
                }
                // Center 50% screen check
                else if (_playerTransform != null && distToRoot < distToPlayer)
                {
                    Vector3 viewportPos = Camera.main.WorldToViewportPoint(rootPos);
                    if (viewportPos.z > 0f)
                    {
                        if (viewportPos.x >= 0.25f && viewportPos.x <= 0.75f &&
                            viewportPos.y >= 0.20f && viewportPos.y <= 0.80f)
                        {
                            shouldFade = true;
                        }
                    }
                }

                if (shouldFade)
                {
                    RegisterObstruction(rootObj);
                }
            }

            // 3. Update cooldowns and restore
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

        private GameObject GetRootObstruction(GameObject obj)
        {
            GameObject rootObj = obj;
            Transform parent = obj.transform.parent;
            while (parent != null && parent.gameObject.layer == _disappearLayer)
            {
                rootObj = parent.gameObject;
                parent = parent.parent;
            }
            return rootObj;
        }

        private void RegisterObstruction(GameObject rootObj)
        {
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

            // Get all colliders to disable them so player can pass through
            obs.colliders = rootObj.GetComponentsInChildren<Collider>(true);
            foreach (var col in obs.colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }

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
                // Restore all colliders
                if (obs.colliders != null)
                {
                    foreach (var col in obs.colliders)
                    {
                        if (col != null)
                        {
                            col.enabled = true;
                        }
                    }
                }

                // Restore original materials
                for (int i = 0; i < obs.renderers.Length; i++)
                {
                    if (obs.renderers[i] != null)
                    {
                        obs.renderers[i].sharedMaterials = obs.originalMaterials[i];
                    }
                }
                _obstructedObjects.Remove(rootObj);
            }
        }

        private void MakeMaterialTransparent(Material mat)
        {
            if (mat == null) return;

            // Preserve original texture and color before changing shader
            Texture mainTex = null;
            if (mat.HasProperty("_BaseMap")) mainTex = mat.GetTexture("_BaseMap");
            else if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");

            Color col = Color.white;
            if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) col = mat.GetColor("_Color");

            // Force change to URP Lit shader to guarantee runtime transparency support
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                mat.shader = urpLit;
            }

            // Set transparency rendering properties for URP Lit shader
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f); // Alpha Blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Restore texture and apply faded color
            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
            }

            mat.SetColor("_BaseColor", new Color(col.r, col.g, col.b, _targetAlpha));
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
