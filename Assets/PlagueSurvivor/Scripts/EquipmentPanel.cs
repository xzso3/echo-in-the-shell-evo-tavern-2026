using TMPro;
using UnityEngine;

namespace PlagueSurvivor
{
    public sealed class EquipmentPanel : MonoBehaviour
    {
        sealed class Cell
        {
            public UnityEngine.UI.Image background, icon;
            public UnityEngine.UI.Outline outline;
            public TMP_Text text;
        }
        PlagueGame game;
        GameObject root;
        TMP_Text stats, capacity, detail, notice, equipLabel, deleteLabel;
        UnityEngine.UI.Button equipButton, deleteButton;
        UnityEngine.UI.GridLayoutGroup grid;
        RectTransform gridRect;
        readonly Cell[] cells = new Cell[PlayerEquipment.Capacity];
        readonly Cell[] slots = new Cell[3];
        int selectedBag = -1, selectedSlot = -1;
        EquipmentItem pendingDelete;
        static readonly Color PanelColor = new Color(.035f,.065f,.085f,.98f);
        static readonly Color Muted = new Color(.52f,.64f,.7f);

        public void Initialize(PlagueGame owner)
        {
            game = owner;
            var canvas = game.status.GetComponentInParent<Canvas>();
            if (!canvas) { Debug.LogError("Inventory requires the HUD Canvas."); return; }
            if (!canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()) canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (!UnityEngine.EventSystems.EventSystem.current)
                new GameObject("Inventory EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            var backdrop = Rect("Inventory", canvas.transform, 0,0,1,1);
            root = backdrop.gameObject;
            Background(backdrop, new Color(.005f,.012f,.02f,.92f));
            var content = Rect("Content", backdrop, .045f,.08f,.955f,.91f);
            // Exact 2/5 : 3/5 outer columns; all padding lives inside each column.
            var left = Rect("Equipment40", content, 0,0,.4f,1);
            var right = Rect("Backpack60", content, .4f,0,1,1);
            Background(left, PanelColor); Background(right, new Color(.025f,.045f,.065f,.98f));
            var leftInner = Rect("Inner", left, .05f,.03f,.95f,.97f);
            var rightInner = Rect("Inner", right, .035f,.03f,.965f,.97f);
            Label("Title", leftInner, "LOADOUT", 30, 0,.90f,1,1);
            Label("Title", rightInner, "BACKPACK", 30, 0,.90f,.60f,1);
            capacity = Label("Capacity", rightInner, "0 / 24", 22, .68f,.90f,1,1);
            capacity.alignment = TextAlignmentOptions.MidlineRight;
            EquipmentSlot[] order = { EquipmentSlot.Weapon, EquipmentSlot.Feet, EquipmentSlot.Module };
            for (int row = 0; row < order.Length; row++)
            {
                int index = (int)order[row];
                float top = .87f - row * .205f;
                var rect = Rect(order[row] + " Slot", leftInner, 0,top-.18f,1,top);
                slots[index] = MakeCell(rect, () => Select(-1,index), true);
            }
            stats = Label("Stats", leftInner, "", 20, 0,.005f,1,.23f);
            gridRect = Rect("Grid24", rightInner, 0,.36f,1,.875f);
            grid = gridRect.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6; grid.spacing = new Vector2(7,7);
            grid.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                cells[i] = MakeCell(Rect("Bag " + (i+1).ToString("00"), gridRect, 0,0,1,1), () => Select(index,-1), false);
            }
            detail = Label("Details", rightInner, "", 21, 0,.135f,1,.35f);
            detail.alignment = TextAlignmentOptions.TopLeft;
            equipButton = Button("EquipAction", rightInner, "Equip", EquipSelected, 0,.015f,.48f,.105f, out equipLabel);
            deleteButton = Button("DeleteAction", rightInner, "Delete", DeleteSelected, .52f,.015f,1,.105f, out deleteLabel);
            deleteButton.GetComponent<UnityEngine.UI.Image>().color = new Color(.35f,.10f,.15f);
            TMP_Text closeText;
            Button("Close", backdrop, "Close  [E / ESC]", () => game.SetEquipmentOpen(false), .78f,.925f,.955f,.985f, out closeText);
            notice = Label("Notice", backdrop, "", 19, .045f,.01f,.955f,.065f);
            game.equipment.Changed += OnInventoryChanged;
            SetVisible(false);
        }

        void LateUpdate() { if (root && root.activeSelf) ResizeGrid(); }
        void ResizeGrid()
        {
            if (!grid) return;
            float side = Mathf.Max(1, Mathf.Min((gridRect.rect.width-35)/6, (gridRect.rect.height-21)/4));
            if (Mathf.Abs(grid.cellSize.x-side) > .1f) grid.cellSize = new Vector2(side,side);
        }
        public void SetVisible(bool visible)
        {
            if (!root) return;
            root.SetActive(visible);
            pendingDelete = null;
            if (visible)
            {
                root.transform.SetAsLastSibling(); selectedBag = selectedSlot = -1;
                notice.text = "Select an item to equip or delete.  Nearby loot is collected automatically.";
                Canvas.ForceUpdateCanvases(); ResizeGrid(); Refresh();
            }
        }
        void Select(int bag, int slot)
        {
            selectedBag = bag; selectedSlot = slot; pendingDelete = null;
            notice.text = ""; Refresh();
        }
        void OnInventoryChanged() { pendingDelete = null; Refresh(); }
        EquipmentItem Selection { get { return selectedBag >= 0 ? game.equipment.At(selectedBag) : selectedSlot >= 0 ? game.equipment.Equipped((EquipmentSlot)selectedSlot) : null; } }
        void EquipSelected()
        {
            if (Selection == null) return;
            bool ok = selectedBag >= 0 ? game.EquipItem(selectedBag) : game.UnequipItem((EquipmentSlot)selectedSlot);
            notice.text = ok ? "Loadout updated." : "Backpack full. Delete an item before unequipping.";
            Refresh();
        }
        void DeleteSelected()
        {
            var item = Selection;
            if (selectedBag < 0 || item == null) return;
            if (!ReferenceEquals(pendingDelete,item))
            {
                pendingDelete = item;
                notice.text = "Delete " + item.Quality + " " + item.Name + " permanently? Click Confirm delete, or select another slot to cancel.";
                Refresh(); return;
            }
            if (game.DeleteInventoryItem(selectedBag)) notice.text = "Item deleted.";
            pendingDelete = null; Refresh();
        }
        public void Refresh()
        {
            if (!stats) return;
            capacity.text = game.equipment.Count + " / " + PlayerEquipment.Capacity;
            capacity.color = game.equipment.Full ? new Color(1,.6f,.3f) : Color.white;
            stats.text = "ATTACK POWER   " + game.EffectiveDamage.ToString("0.#") + "  (+" + game.equipment.DamageBonus.ToString("0.#") + ")\n"
                + "MOVE SPEED   " + game.EffectiveMoveSpeed.ToString("0.00") + "  (+" + game.equipment.MoveBonus.ToString("0.#") + "%)\n"
                + "ATTACK SPEED   " + (1/game.EffectiveAttackInterval).ToString("0.00") + "/s\n"
                + "INTERVAL   " + game.EffectiveAttackInterval.ToString("0.00") + "s";
            for (int i = 0; i < cells.Length; i++) Draw(cells[i], game.equipment.At(i), selectedBag == i, "");
            for (int i = 0; i < slots.Length; i++) Draw(slots[i], game.equipment.Equipped((EquipmentSlot)i), selectedSlot == i, ((EquipmentSlot)i).ToString().ToUpperInvariant());
            var item = Selection;
            if (item == null) detail.text = "<color=#8DA6B6>Select equipment to inspect</color>\n<size=18>One item per cell. " +
                (game.bossEncounter ? "Collect arena supplies to find gear." : "Defeat enemies to find gear.") + "\nGray / Silver / Blue / Purple / Gold</size>";
            else
            {
                var current = game.equipment.Equipped(item.Slot);
                float difference = item.Value - (current == null ? 0 : current.Value);
                string color = ColorUtility.ToHtmlStringRGB(game.equipmentCatalog.Tier(item.Quality).color);
                detail.text = "<color=#" + color + ">" + item.Quality.ToString().ToUpperInvariant() + "  " + item.Name + "</color>\n"
                    + item.Affix + "\n<size=18>" + (selectedSlot >= 0 ? "Currently equipped" : "Compared to equipped: " + (difference >= 0 ? "+" : "") + difference.ToString("0.#") + item.Suffix + " " + item.StatName) + "</size>";
            }
            equipButton.interactable = item != null;
            deleteButton.interactable = selectedBag >= 0 && item != null;
            equipLabel.text = selectedSlot >= 0 ? "Unequip" : "Equip / Replace";
            deleteLabel.text = pendingDelete != null ? "Confirm delete" : "Delete";
        }
        void Draw(Cell cell, EquipmentItem item, bool selected, string slotName)
        {
            cell.icon.sprite = item == null ? null : game.equipmentCatalog.Icon(item);
            cell.icon.enabled = item != null;
            cell.background.color = item == null ? new Color(.07f,.11f,.14f) : new Color(.1f,.15f,.18f);
            cell.outline.effectColor = selected ? Color.white : item != null ? game.equipmentCatalog.Tier(item.Quality).color : new Color(.16f,.26f,.31f);
            cell.outline.effectDistance = Vector2.one * (selected ? 3 : 1);
            cell.text.text = slotName.Length == 0 ? (item == null ? "" : item.Quality.ToString().Substring(0,1))
                : "<size=17><color=#8DA6B6>" + slotName + "</color></size>\n" + (item == null ? "Empty slot" : item.Name + "\n<size=17>" + item.Affix + "</size>");
        }
        Cell MakeCell(RectTransform rect, UnityEngine.Events.UnityAction onClick, bool loadout)
        {
            var cell = new Cell();
            cell.background = Background(rect, PanelColor);
            cell.outline = rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = cell.background; button.onClick.AddListener(onClick); DisableNavigation(button);
            var iconRect = Rect("Icon", rect, .04f,.05f,loadout ? .31f : .96f,.95f);
            cell.icon = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            cell.icon.preserveAspect = true; cell.icon.raycastTarget = false;
            cell.text = Label("Label", rect, "", loadout ? 20 : 13, loadout ? .35f : .76f, .02f, .97f, loadout ? .98f : .25f);
            if (!loadout) { cell.text.alignment = TextAlignmentOptions.BottomRight; cell.text.fontStyle = FontStyles.Bold; }
            return cell;
        }
        UnityEngine.UI.Button Button(string name, Transform parent, string text, UnityEngine.Events.UnityAction action, float x0, float y0, float x1, float y1, out TMP_Text label)
        {
            var rect = Rect(name,parent,x0,y0,x1,y1);
            var image = Background(rect, new Color(.055f,.29f,.34f));
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image; button.onClick.AddListener(action); DisableNavigation(button);
            label = Label("Label",rect,text,21,.03f,0,.97f,1); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
        static void DisableNavigation(UnityEngine.UI.Button button)
        { var n = button.navigation; n.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = n; }
        TMP_Text Label(string name, Transform parent, string text, float size, float x0, float y0, float x1, float y1)
        {
            var label = Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = game.status.font; label.text = text; label.fontSize = size;
            label.enableAutoSizing = true; label.fontSizeMin = size * .72f; label.fontSizeMax = size;
            label.color = new Color(.83f,.94f,.98f); label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }
        static UnityEngine.UI.Image Background(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; return image; }
        static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        void OnDestroy()
        { if (game) game.equipment.Changed -= OnInventoryChanged; if (root) Destroy(root); }
    }
}
