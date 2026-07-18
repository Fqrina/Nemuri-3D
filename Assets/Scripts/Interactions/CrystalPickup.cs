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

        // Audio Volume (Set manually between 0.0f and 1.0f)
        private float _gemsVolume = 10.0f;

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

            AudioClip gemsClip = Resources.Load<AudioClip>("Gems");
            if (gemsClip != null)
            {
                AudioSource.PlayClipAtPoint(gemsClip, transform.position, _gemsVolume);
            }

            gameObject.SetActive(false);

            if (gameObject.name == "dobj.001")
            {
                if (Nemuri.Scenes.NocturneIntroController.Instance != null)
                {
                    Nemuri.Scenes.NocturneIntroController.Instance.TriggerSomniaSeedCollectedDialogue();
                }
            }
            else if (gameObject.name == "dobj")
            {
                if (Nemuri.Scenes.NocturneIntroController.Instance != null)
                {
                    Nemuri.Scenes.NocturneIntroController.Instance.TriggerCrescentTearCollectedDialogue();
                }
            }
            else if (gameObject.name == "dobj.002")
            {
                if (Nemuri.Scenes.NocturneIntroController.Instance != null)
                {
                    Nemuri.Scenes.NocturneIntroController.Instance.TriggerPuzzle3CollectedDialogue();
                }
            }
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
