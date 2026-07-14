using UnityEngine;
using Nemuri.Player;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(SphereCollider))]
    public class CrystalPickup : MonoBehaviour
    {
        [SerializeField] private int _crystalIndex;
        [SerializeField] private bool _requiresMinigame;
        private bool _collected;

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (_requiresMinigame) return;
            if (!IsPlayer(other)) return;

            Collect();
        }

        public void Collect()
        {
            if (_collected) return;

            EnsurePuzzleManager();

            _collected = true;
            PuzzleManager.Instance.CollectCrystal(_crystalIndex);
            gameObject.SetActive(false);
        }

        private static void EnsurePuzzleManager()
        {
            if (PuzzleManager.Instance != null) return;
            new GameObject("PuzzleManager").AddComponent<PuzzleManager>();
        }

        private static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            return other.TryGetComponent(out PlayerMovement _);
        }
    }
}
