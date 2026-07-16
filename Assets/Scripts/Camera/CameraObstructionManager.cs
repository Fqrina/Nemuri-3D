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
            public Vector3 cachedCenter;
            public Vector2[] cachedCornersXZ;
        }

        [Header("Settings")]
        [SerializeField] private float _cameraSphereRadius = 1.8f;
        [SerializeField] private string _disappearLayerName = "CameraDisappear";
        [SerializeField] private float _fadeCooldown = 0.35f;
        [SerializeField] private float _targetAlpha = 0.25f;
        [SerializeField] private float _cameraProximityRadius = 3.0f;
        [SerializeField] private float _triangleWidth = 2.2f;

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

            if (_playerTransform == null)
            {
                return;
            }

            Vector3 cameraPos = transform.position;
            Vector3 playerPos = _playerTransform.position;

            Vector2 cameraXZ = new Vector2(cameraPos.x, cameraPos.z);
            Vector2 playerXZ = new Vector2(playerPos.x, playerPos.z);

            Vector2 toPlayer = playerXZ - cameraXZ;
            Vector2 dir = toPlayer.normalized;
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);

            Vector2 baseCenter = playerXZ + dir * 1.5f;

            Vector2 vertexA = cameraXZ;
            Vector2 vertexB = baseCenter - perpendicular * _triangleWidth;
            Vector2 vertexC = baseCenter + perpendicular * _triangleWidth;

            Collider[] colliders = Physics.OverlapSphere(playerPos, 15f, _layerMask);
            Collider[] cameraColliders = Physics.OverlapSphere(cameraPos, _cameraProximityRadius, _layerMask);
            RaycastHit[] hits = Physics.RaycastAll(cameraPos, (playerPos - cameraPos).normalized, Vector3.Distance(cameraPos, playerPos) + 1.0f, _layerMask);

            HashSet<GameObject> detectedRoots = new HashSet<GameObject>();
            HashSet<Collider> uniqueColliders = new HashSet<Collider>();

            foreach (var col in colliders)
            {
                if (col != null)
                {
                    uniqueColliders.Add(col);
                }
            }

            foreach (var col in cameraColliders)
            {
                if (col != null)
                {
                    uniqueColliders.Add(col);
                    GameObject rootObj = GetRootObstruction(col.gameObject);
                    detectedRoots.Add(rootObj);
                }
            }

            foreach (var col in uniqueColliders)
            {
                GameObject rootObj = GetRootObstruction(col.gameObject);
                if (detectedRoots.Contains(rootObj))
                {
                    continue;
                }

                if (rootObj.transform.position.y < playerPos.y - 0.5f)
                {
                    continue;
                }

                Vector3 rootWorldPos = rootObj.transform.position;
                Vector2 rootXZ = new Vector2(rootWorldPos.x, rootWorldPos.z);

                Vector3 colCenterWorld = col.bounds.center;
                Vector2 colCenterXZ = new Vector2(colCenterWorld.x, colCenterWorld.z);

                if (IsPointInTriangle(rootXZ, vertexA, vertexB, vertexC) ||
                    IsPointInTriangle(colCenterXZ, vertexA, vertexB, vertexC) ||
                    IsAnyCornerInTriangle(col.bounds, vertexA, vertexB, vertexC))
                {
                    detectedRoots.Add(rootObj);
                }
            }

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                GameObject rootObj = GetRootObstruction(hit.collider.gameObject);
                if (rootObj.transform.position.y < playerPos.y - 0.5f)
                {
                    continue;
                }

                detectedRoots.Add(rootObj);
            }

            foreach (var rootObj in detectedRoots)
            {
                RegisterObstruction(rootObj);
            }

            foreach (var kvp in _obstructedObjects)
            {
                GameObject rootObj = kvp.Key;
                ObstructedObject obs = kvp.Value;

                bool inside = Vector3.Distance(cameraPos, obs.cachedCenter) <= _cameraProximityRadius;

                if (!inside)
                {
                    Vector3 rootWorldPos = rootObj.transform.position;
                    Vector2 rootXZ = new Vector2(rootWorldPos.x, rootWorldPos.z);
                    Vector2 centerXZ = new Vector2(obs.cachedCenter.x, obs.cachedCenter.z);

                    inside = IsPointInTriangle(rootXZ, vertexA, vertexB, vertexC) ||
                             IsPointInTriangle(centerXZ, vertexA, vertexB, vertexC);

                    if (!inside && obs.cachedCornersXZ != null)
                    {
                        foreach (var corner in obs.cachedCornersXZ)
                        {
                            if (IsPointInTriangle(corner, vertexA, vertexB, vertexC))
                            {
                                inside = true;
                                break;
                            }
                        }
                    }

                    if (!inside)
                    {
                        inside = LineIntersectsBoundsXZ(cameraXZ, playerXZ, obs.cachedCenter, obs.cachedCornersXZ);
                    }
                }

                if (inside)
                {
                    obs.cooldownTimer = _fadeCooldown;
                }
            }

            List<GameObject> toRestore = new List<GameObject>();
            List<GameObject> activeKeys = new List<GameObject>(_obstructedObjects.Keys);

            foreach (var key in activeKeys)
            {
                var obs = _obstructedObjects[key];
                if (!detectedRoots.Contains(key))
                {
                    obs.cooldownTimer -= Time.deltaTime;
                    if (obs.cooldownTimer <= 0f)
                    {
                        toRestore.Add(key);
                    }
                }
            }

            foreach (var key in toRestore)
            {
                RestoreObstruction(key);
            }
        }

        private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }

        private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private bool IsAnyCornerInTriangle(Bounds b, Vector2 a, Vector2 bVert, Vector2 c)
        {
            Vector2 corner1 = new Vector2(b.min.x, b.min.z);
            Vector2 corner2 = new Vector2(b.max.x, b.min.z);
            Vector2 corner3 = new Vector2(b.min.x, b.max.z);
            Vector2 corner4 = new Vector2(b.max.x, b.max.z);

            return IsPointInTriangle(corner1, a, bVert, c) ||
                   IsPointInTriangle(corner2, a, bVert, c) ||
                   IsPointInTriangle(corner3, a, bVert, c) ||
                   IsPointInTriangle(corner4, a, bVert, c);
        }

        private bool LineIntersectsBoundsXZ(Vector2 cameraXZ, Vector2 playerXZ, Vector3 cachedCenter, Vector2[] corners)
        {
            Vector2 p = new Vector2(cachedCenter.x, cachedCenter.z);
            Vector2 ab = playerXZ - cameraXZ;
            Vector2 ap = p - cameraXZ;

            float abLenSq = ab.sqrMagnitude;
            if (abLenSq < 0.0001f)
            {
                return false;
            }

            float t = Vector2.Dot(ap, ab) / abLenSq;
            t = Mathf.Clamp01(t);

            Vector2 projection = cameraXZ + t * ab;
            float distSq = (p - projection).sqrMagnitude;

            float maxRadius = 1.0f;
            if (corners != null && corners.Length > 0)
            {
                maxRadius = 0f;
                foreach (var corner in corners)
                {
                    maxRadius = Mathf.Max(maxRadius, (p - corner).magnitude);
                }
            }

            return distSq <= (maxRadius * maxRadius);
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
                existing.cooldownTimer = _fadeCooldown;
                return;
            }

            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            ObstructedObject obs = new ObstructedObject();
            obs.gameObject = rootObj;
            obs.renderers = renderers;
            obs.cooldownTimer = _fadeCooldown;
            obs.originalMaterials = new Material[renderers.Length][];

            Bounds combinedBounds = new Bounds();
            bool hasBounds = false;
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(r.bounds);
                    }
                }
            }

            if (hasBounds)
            {
                obs.cachedCenter = combinedBounds.center;
                Vector3 min = combinedBounds.min;
                Vector3 max = combinedBounds.max;
                obs.cachedCornersXZ = new Vector2[]
                {
                    new Vector2(min.x, min.z),
                    new Vector2(max.x, min.z),
                    new Vector2(min.x, max.z),
                    new Vector2(max.x, max.z)
                };
            }
            else
            {
                obs.cachedCenter = rootObj.transform.position;
                obs.cachedCornersXZ = new Vector2[] { new Vector2(rootObj.transform.position.x, rootObj.transform.position.z) };
            }

            obs.colliders = rootObj.GetComponentsInChildren<Collider>(true);

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
            if (mat == null)
            {
                return;
            }

            Texture mainTex = null;
            if (mat.HasProperty("_BaseMap"))
            {
                mainTex = mat.GetTexture("_BaseMap");
            }
            else if (mat.HasProperty("_MainTex"))
            {
                mainTex = mat.GetTexture("_MainTex");
            }

            Color col = Color.white;
            if (mat.HasProperty("_BaseColor"))
            {
                col = mat.GetColor("_BaseColor");
            }
            else if (mat.HasProperty("_Color"))
            {
                col = mat.GetColor("_Color");
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                mat.shader = urpLit;
            }

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", mainTex);
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", mainTex);
                }
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
            List<GameObject> activeKeys = new List<GameObject>(_obstructedObjects.Keys);
            foreach (var key in activeKeys)
            {
                RestoreObstruction(key);
            }
            _obstructedObjects.Clear();
        }
    }
}
