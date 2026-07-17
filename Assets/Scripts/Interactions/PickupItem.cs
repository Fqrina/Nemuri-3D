using UnityEngine;
using Nemuri.Inventory;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private string _displayName = "item";
        [SerializeField] private Sprite _itemIcon;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            // configure prompt text but keep the inspector's hold seconds setting
            _interactable.PromptText = "Hold E to Pick Up " + _displayName;
            _interactable.OnInteract.AddListener(OnPickUp);
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.OnInteract.RemoveListener(OnPickUp);
            }
        }

        private void OnPickUp()
        {
            if (HotbarInventory.Instance != null)
            {
                bool added = HotbarInventory.Instance.AddItem(_displayName, _itemIcon);
                if (added)
                {
                    _interactable.enabled = false;
                    _interactable.DismissInteraction();
                    Destroy(gameObject);
                }
            }
        }
    }
}
