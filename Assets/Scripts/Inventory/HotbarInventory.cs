using UnityEngine;
using UnityEngine.InputSystem;

namespace Nemuri.Inventory
{
    public enum ItemGroup
    {
        Group1, // items 1-3
        Group2, // items 4-6
        Group3  // items 7-9
    }

    public class HotbarItem
    {
        public string displayName;
        public Sprite icon;
        public string description;
        public ItemGroup group;
        public int itemId;

        public HotbarItem(string displayName, Sprite icon, string description, ItemGroup group, int itemId)
        {
            this.displayName = displayName;
            this.icon = icon;
            this.description = description;
            this.group = group;
            this.itemId = itemId;
        }
    }

    public class HotbarInventory : MonoBehaviour
    {
        public static HotbarInventory Instance { get; private set; }

        private const int TotalSlots = 9;
        private readonly HotbarItem[] _slots = new HotbarItem[TotalSlots];
        private int _selectedIndex = 0;

        public event System.Action<int> OnSlotSelected;
        public event System.Action OnInventoryUpdated;

        private readonly bool[,] _collectedGroupItems = new bool[3, 3]; // [group, itemIdx (0-2)]

        public bool IsLocked { get; set; }

        public int SelectedIndex => _selectedIndex;
        public int SlotCount => TotalSlots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (IsLocked) return;
            HandleSelectionInput();
            HandleCraftingInput();
        }

        private void HandleCraftingInput()
        {
        }

        public void CraftGroup(ItemGroup group)
        {
            int g = (int)group;

            // clear items of this group from inventory slots
            for (int i = 0; i < TotalSlots; i++)
            {
                if (_slots[i] != null && _slots[i].group == group)
                {
                    _slots[i] = null;
                }
            }

            // reset collection state
            _collectedGroupItems[g, 0] = false;
            _collectedGroupItems[g, 1] = false;
            _collectedGroupItems[g, 2] = false;

            OnInventoryUpdated?.Invoke();
        }

        public void AbsorbGroupAndGrantCrystal(ItemGroup group, string crystalName, Sprite crystalIcon, string crystalDesc)
        {
            int g = (int)group;

            // Delete / Absorb all 3 items belonging to this group from inventory slots
            for (int i = 0; i < TotalSlots; i++)
            {
                if (_slots[i] != null && _slots[i].group == group)
                {
                    _slots[i] = null;
                }
            }

            // Grant 1 crystal item into the first empty slot
            for (int i = 0; i < TotalSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = new HotbarItem(
                        displayName: crystalName,
                        icon: crystalIcon,
                        description: crystalDesc,
                        group: group,
                        itemId: 900 + g
                    );
                    Debug.Log(string.Format("[HotbarInventory] Group {0} items absorbed! Granted crystal: {1} at slot {2}", group, crystalName, i));
                    break;
                }
            }

            OnInventoryUpdated?.Invoke();
        }

        public bool IsGroupFullyCollected(ItemGroup group)
        {
            int g = (int)group;
            return _collectedGroupItems[g, 0] && _collectedGroupItems[g, 1] && _collectedGroupItems[g, 2];
        }

        public int GetCollectedCount(ItemGroup group)
        {
            int g = (int)group;
            int count = 0;
            if (_collectedGroupItems[g, 0]) count++;
            if (_collectedGroupItems[g, 1]) count++;
            if (_collectedGroupItems[g, 2]) count++;
            return count;
        }

        private void HandleSelectionInput()
        {
            int previousIndex = _selectedIndex;

            // scroll wheel input
            if (Mouse.current != null)
            {
                Vector2 scrollValue = Mouse.current.scroll.ReadValue();
                if (scrollValue.y > 0f)
                {
                    _selectedIndex = (_selectedIndex - 1 + TotalSlots) % TotalSlots;
                }
                else if (scrollValue.y < 0f)
                {
                    _selectedIndex = (_selectedIndex + 1) % TotalSlots;
                }
            }

            // arrow keys input
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                {
                    _selectedIndex = (_selectedIndex - 1 + TotalSlots) % TotalSlots;
                }
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    _selectedIndex = (_selectedIndex + 1) % TotalSlots;
                }

                // // number keys selection
                // if (Keyboard.current.digit1Key.wasPressedThisFrame) _selectedIndex = 0;
                // if (Keyboard.current.digit2Key.wasPressedThisFrame) _selectedIndex = 1;
                // if (Keyboard.current.digit3Key.wasPressedThisFrame) _selectedIndex = 2;
                // if (Keyboard.current.digit4Key.wasPressedThisFrame) _selectedIndex = 3;
                // if (Keyboard.current.digit5Key.wasPressedThisFrame) _selectedIndex = 4;
                // if (Keyboard.current.digit6Key.wasPressedThisFrame) _selectedIndex = 5;
                // if (Keyboard.current.digit7Key.wasPressedThisFrame) _selectedIndex = 6;
                // if (Keyboard.current.digit8Key.wasPressedThisFrame) _selectedIndex = 7;
                // if (Keyboard.current.digit9Key.wasPressedThisFrame) _selectedIndex = 8;
            }

            if (_selectedIndex != previousIndex)
            {
                OnSlotSelected?.Invoke(_selectedIndex);
            }
        }

        public bool AddItem(string displayName, Sprite icon, string description, ItemGroup group, int itemId)
        {
            for (int i = 0; i < TotalSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = new HotbarItem(displayName, icon, description, group, itemId);

                    // mark item as collected in the group array
                    int gIndex = (int)group;
                    int itemIdx = Mathf.Clamp(itemId - 1, 0, 2);
                    _collectedGroupItems[gIndex, itemIdx] = true;

                    Debug.Log("[HotbarInventory] Collected: " + displayName + " for Group: " + group + " (Index: " + itemId + ")");

                    if (IsGroupFullyCollected(group))
                    {
                        Debug.Log("[HotbarInventory] Group " + group + " is fully collected!");
                    }

                    OnInventoryUpdated?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public HotbarItem GetItemAt(int index)
        {
            if (index < 0 || index >= TotalSlots) return null;
            return _slots[index];
        }

        public HotbarItem GetSelectedItem()
        {
            return _slots[_selectedIndex];
        }
    }
}
