using System.Collections.Generic;
using UnityEngine;
using Nemuri.Player;

namespace Nemuri.CameraEffects
{
    public class CameraObstructionManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _cameraSphereRadius = 1.8f;
        [SerializeField] private string _disappearLayerName = "CameraDisappear";

        private Transform _playerTransform;
        private int _disappearLayer;
        private int _layerMask;

        private HashSet<Renderer> _currentlyHiddenRenderers = new HashSet<Renderer>();
        private HashSet<Renderer> _newlyHiddenRenderers = new HashSet<Renderer>();

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

            _newlyHiddenRenderers.Clear();

            Vector3 cameraPos = transform.position;

            // 1. Hide objects that are overlapping or extremely close to the camera
            Collider[] closeColliders = Physics.OverlapSphere(cameraPos, _cameraSphereRadius, _layerMask);
            foreach (var col in closeColliders)
            {
                if (col != null)
                {
                    HideObjectRenderers(col.gameObject);
                }
            }

            // 2. Hide objects blocking the line of sight between camera and active player
            if (_playerTransform != null)
            {
                Vector3 playerPos = _playerTransform.position + Vector3.up * 1f; // target chest/head level
                float distance = Vector3.Distance(cameraPos, playerPos);
                Vector3 direction = (playerPos - cameraPos).normalized;

                RaycastHit[] hits = Physics.RaycastAll(cameraPos, direction, distance, _layerMask);
                foreach (var hit in hits)
                {
                    if (hit.collider != null)
                    {
                        HideObjectRenderers(hit.collider.gameObject);
                    }
                }
            }

            // 3. Restore renderers that are no longer obstructing
            List<Renderer> toRestore = new List<Renderer>();
            foreach (var renderer in _currentlyHiddenRenderers)
            {
                if (!_newlyHiddenRenderers.Contains(renderer))
                {
                    toRestore.Add(renderer);
                }
            }

            foreach (var renderer in toRestore)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
                _currentlyHiddenRenderers.Remove(renderer);
            }

            // 4. Update the currently hidden set with the newly hidden ones
            foreach (var renderer in _newlyHiddenRenderers)
            {
                _currentlyHiddenRenderers.Add(renderer);
            }
        }

        private void HideObjectRenderers(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    renderer.enabled = false;
                    _newlyHiddenRenderers.Add(renderer);
                }
            }

            Transform current = obj.transform.parent;
            while (current != null && current.gameObject.layer == _disappearLayer)
            {
                Renderer[] parentRenderers = current.GetComponentsInChildren<Renderer>(true);
                foreach (var r in parentRenderers)
                {
                    if (r != null && r.enabled)
                    {
                        r.enabled = false;
                        _newlyHiddenRenderers.Add(r);
                    }
                }
                current = current.parent;
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
            foreach (var renderer in _currentlyHiddenRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
            _currentlyHiddenRenderers.Clear();
        }
    }
}
