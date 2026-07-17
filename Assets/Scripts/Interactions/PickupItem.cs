using UnityEngine;
using Nemuri.Inventory;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private string _displayName = "item";
        [SerializeField] private Sprite _itemIcon;
        [TextArea(2, 5)] [SerializeField] private string _description = "This is a description.";

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
                bool added = HotbarInventory.Instance.AddItem(_displayName, _itemIcon, _description);
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
