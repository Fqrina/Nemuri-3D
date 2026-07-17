using UnityEngine;
using UnityEngine.InputSystem;

namespace Nemuri.Inventory
{
    public class HotbarItem
    {
        public string displayName;
        public Sprite icon;

        public HotbarItem(string displayName, Sprite icon)
        {
            this.displayName = displayName;
            this.icon = icon;
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
            HandleSelectionInput();
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

        public bool AddItem(string displayName, Sprite icon)
        {
            for (int i = 0; i < TotalSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = new HotbarItem(displayName, icon);
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
