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
        [SerializeField] private string _disappearLayerName = "CameraDisappear";
        [SerializeField] private float _fadeCooldown = 0.25f;
        [SerializeField] private float _targetAlpha = 0.2f;

        private Transform _playerTransform;
        private Camera _cam;
        private int _disappearLayer = -1;
        private int _layerMask = 0;

        private static readonly string[] TargetNames = new string[]
        {
            "Cube.003",
            "Cylinder.001", "Cylinder.002", "Cylinder.003",
            "Cylinder.010", "Cylinder.011", "Cylinder.012", "Cylinder.013",
            "Cylinder.014", "Cylinder.015", "Cylinder.016",
            "Sphere"
        };

        private Dictionary<GameObject, ObstructedObject> _obstructedObjects = new Dictionary<GameObject, ObstructedObject>();
        private List<Renderer> _targetRenderers = new List<Renderer>();
        private float _refreshTimer = 0f;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            int l1 = LayerMask.NameToLayer("CameraDissappear");
            int l2 = LayerMask.NameToLayer("CameraDisappear");
            int l3 = LayerMask.NameToLayer(_disappearLayerName);

            if (l1 != -1) { _layerMask |= (1 << l1); _disappearLayer = l1; }
            if (l2 != -1) { _layerMask |= (1 << l2); if (_disappearLayer == -1) _disappearLayer = l2; }
            if (l3 != -1) { _layerMask |= (1 << l3); }

            RefreshTargetRenderers();
        }

        private void RefreshTargetRenderers()
        {
            _targetRenderers.Clear();
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r == null || !r.gameObject.scene.isLoaded) continue;

                bool match = false;
                if (_layerMask != 0 && (_layerMask & (1 << r.gameObject.layer)) != 0)
                {
                    match = true;
                }

                if (!match)
                {
                    string name = r.gameObject.name;
                    foreach (string t in TargetNames)
                    {
                        if (name.Equals(t, System.StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                if (match)
                {
                    _targetRenderers.Add(r);
                }
            }
        }

        private void Update()
        {
            FindActivePlayer();

            if (_cam == null) _cam = Camera.main;
            if (_playerTransform == null || _cam == null) return;

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer > 2.0f)
            {
                _refreshTimer = 0f;
                RefreshTargetRenderers();
            }

            Vector3 cameraPos = _cam.transform.position;
            Vector3 playerHead = _playerTransform.position + Vector3.up * 1.5f;

            Vector3 rayDir = (playerHead - cameraPos);
            float maxDist = rayDir.magnitude;
            if (maxDist < 0.2f) return;
            rayDir.Normalize();

            Vector3 camRight = _cam.transform.right;

            // Define 3 rays covering center, left, and right camera view to player
            Ray rayCenter = new Ray(cameraPos, rayDir);
            Ray rayLeft = new Ray(cameraPos, (playerHead - camRight * 1.5f - cameraPos).normalized);
            Ray rayRight = new Ray(cameraPos, (playerHead + camRight * 1.5f - cameraPos).normalized);

            HashSet<GameObject> activeHits = new HashSet<GameObject>();

            foreach (Renderer r in _targetRenderers)
            {
                if (r == null || !r.gameObject.activeInHierarchy || !r.enabled) continue;

                // Check bounding box ray intersection mathematically
                bool centerHit = r.bounds.IntersectRay(rayCenter, out float d1) && d1 > 0.5f && d1 < maxDist - 0.5f;
                bool leftHit = r.bounds.IntersectRay(rayLeft, out float d2) && d2 > 0.5f && d2 < maxDist - 0.5f;
                bool rightHit = r.bounds.IntersectRay(rayRight, out float d3) && d3 > 0.5f && d3 < maxDist - 0.5f;

                if (centerHit || leftHit || rightHit)
                {
                    GameObject rootObj = GetRootObject(r.gameObject);
                    activeHits.Add(rootObj);
                }
            }

            // Register newly detected obstructions
            foreach (GameObject hitObj in activeHits)
            {
                RegisterObstruction(hitObj);
            }

            // Update timers on existing obstructed objects
            List<GameObject> toRestore = new List<GameObject>();
            foreach (var kvp in _obstructedObjects)
            {
                GameObject rootObj = kvp.Key;
                ObstructedObject obs = kvp.Value;

                if (activeHits.Contains(rootObj))
                {
                    obs.cooldownTimer = _fadeCooldown;
                }
                else
                {
                    obs.cooldownTimer -= Time.deltaTime;
                    if (obs.cooldownTimer <= 0f)
                    {
                        toRestore.Add(rootObj);
                    }
                }
            }

            // Restore objects that are no longer blocking
            foreach (GameObject key in toRestore)
            {
                RestoreObstruction(key);
            }
        }

        private GameObject GetRootObject(GameObject obj)
        {
            if (obj == null) return null;
            Transform parent = obj.transform.parent;
            if (parent != null && (parent.name.StartsWith("Cylinder") || parent.name.StartsWith("Cube") || parent.gameObject.layer == _disappearLayer))
            {
                return parent.gameObject;
            }
            return obj;
        }

        private void RegisterObstruction(GameObject rootObj)
        {
            if (rootObj == null) return;
            if (_obstructedObjects.TryGetValue(rootObj, out ObstructedObject existing))
            {
                existing.cooldownTimer = _fadeCooldown;
                return;
            }

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
                    if (obs.renderers[i] != null && obs.originalMaterials[i] != null)
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

            mat.SetFloat("_Surface", 1f); // 1 is transparent in URP
            mat.SetFloat("_Blend", 0f);   // 0 is alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            Color c = mat.color;
            c.a = _targetAlpha;
            mat.color = c;

            if (mat.HasProperty("_BaseColor"))
            {
                Color bc = mat.GetColor("_BaseColor");
                bc.a = _targetAlpha;
                mat.SetColor("_BaseColor", bc);
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
            List<GameObject> activeKeys = new List<GameObject>(_obstructedObjects.Keys);
            foreach (var key in activeKeys)
            {
                RestoreObstruction(key);
            }
            _obstructedObjects.Clear();
        }
    }
}
