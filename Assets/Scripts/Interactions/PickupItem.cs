using UnityEngine;
using Nemuri.Inventory;
using Nemuri.UI;

namespace Nemuri.Interactions
{
    [RequireComponent(typeof(Interactable))]
    public class PickupItem : MonoBehaviour
    {
        [SerializeField] private string _displayName = "item";
        [SerializeField] private Sprite _itemIcon;
        [TextArea(2, 5)] [SerializeField] private string _description = "This is a description.";

        [Header("Vision Settings")]
        [SerializeField] private Sprite _visionSprite;
        [SerializeField] private TextAsset _visionDialogueJson;

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
            Debug.Log("[PickupItem] OnPickUp triggered on GameObject: " + gameObject.name);
            if (HotbarInventory.Instance != null)
            {
                bool added = HotbarInventory.Instance.AddItem(_displayName, _itemIcon, _description);
                if (added)
                {
                    Debug.Log("[PickupItem] Added " + _displayName + " to inventory. Destroying GameObject.");
                    
                    // trigger vision if VisionManager is present
                    if (VisionManager.Instance != null)
                    {
                        VisionManager.Instance.PlayVision(_displayName, _visionSprite, _visionDialogueJson);
                    }

                    _interactable.enabled = false;
                    _interactable.DismissInteraction();
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("[PickupItem] Failed to add " + _displayName + " to inventory (inventory full?).");
                }
            }
            else
            {
                Debug.LogError("[PickupItem] HotbarInventory.Instance is null!");
            }
        }
    }
}
