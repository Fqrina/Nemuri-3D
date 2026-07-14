using UnityEngine;

namespace Nemuri.Interactions
{
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        [SerializeField] private bool[] _crystalsCollected = new bool[3];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void CollectCrystal(int index)
        {
            if (index < 0 || index >= _crystalsCollected.Length) return;
            if (_crystalsCollected[index]) return;

            _crystalsCollected[index] = true;
        }

        public bool AreAllCrystalsCollected()
        {
            foreach (bool b in _crystalsCollected)
            {
                if (!b) return false;
            }
            return true;
        }
    }
}
