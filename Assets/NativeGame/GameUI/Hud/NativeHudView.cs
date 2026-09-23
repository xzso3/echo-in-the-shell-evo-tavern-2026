using System;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // A frame of run state. NativeHud still owns input and the update cadence.
    public readonly struct NativeHudDisplay
    {
        public readonly float Health, MaxHealth, ElapsedSeconds;
        public readonly bool AutoFire;
        public readonly string Objective, InteractionPrompt;
        public readonly int Kills, Sync, Difference;
        public readonly NativeHudExtras Extras;

        // Retain the GF01 call until INT supplies the extended live snapshot.
        public NativeHudDisplay(float health, float maxHealth, bool autoFire, string objective,
            string interactionPrompt, float elapsedSeconds, int kills, int sync, int difference)
            : this(health, maxHealth, autoFire, objective, interactionPrompt, elapsedSeconds,
                kills, sync, difference, default(NativeHudExtras)) { }

        public NativeHudDisplay(float health, float maxHealth, bool autoFire, string objective,
            string interactionPrompt, float elapsedSeconds, int kills, int sync, int difference,
            NativeHudExtras extras)
        {
            Health = health;
            MaxHealth = maxHealth;
            AutoFire = autoFire;
            Objective = objective;
            InteractionPrompt = interactionPrompt;
            ElapsedSeconds = elapsedSeconds;
            Kills = kills;
            Sync = sync;
            Difference = difference;
            Extras = extras;
        }
    }

    // Presentation adapter only. "安全通讯" means the local tablet can open;
    // configured credentials are never treated as proof of a live AI connection.
    public readonly struct NativeHudExtras
    {
        static readonly int RequiredMemoryCount = Enum.GetValues(typeof(NativeMemoryKind)).Length;
        public readonly bool HasState, ShowMemoryClues, PrivateMemory, SystemMemory, InitialEcho;
        public readonly bool HasEquipmentSource, EquipmentActive, TabletAvailable;
        public readonly int MemoryCount, MemoryGoal;
        public readonly string EquipmentTitle, EquipmentHint;

        NativeHudExtras(bool showClues, int count, int goal, bool privateMemory,
            bool systemMemory, bool initialEcho, bool hasEquipment, string gear,
            string hint, bool active, bool tabletAvailable)
        {
            HasState = true;
            ShowMemoryClues = showClues;
            MemoryCount = count;
            MemoryGoal = goal;
            PrivateMemory = privateMemory;
            SystemMemory = systemMemory;
            InitialEcho = initialEcho;
            HasEquipmentSource = hasEquipment;
            EquipmentTitle = gear;
            EquipmentHint = hint;
            EquipmentActive = active;
            TabletAvailable = tabletAvailable;
        }

        public static NativeHudExtras FromRun(NativeRunController run, NativeEquipment equipment,
            bool tabletAvailable)
        {
            var quest = run ? run.quest : null;
            var narrative = run ? run.narrative : null;
            int goal = RequiredMemoryCount;
            int count = narrative ? narrative.MemoryCount : 0;
            bool showClues = quest && narrative && quest.Active && count < goal;
            string gear = null, hint = null;
            bool active = false;
            if (equipment)
            {
                if (!equipment.HasCoil)
                {
                    gear = "装备未获取";
                    hint = "寻找脉冲线圈";
                }
                else if (!equipment.Equipped)
                {
                    gear = "脉冲线圈已拾取";
                    hint = "F 装备";
                }
                else
                {
                    gear = "脉冲线圈  伤害 +" + equipment.damageBonus.ToString("0.#");
                    hint = equipment.OverclockActive
                        ? "超频生效  " + equipment.OverclockSecondsLeft.ToString("0.0") + " 秒"
                        : "F 卸下 / Q 超频 " + equipment.overclockDuration.ToString("0.#") + " 秒";
                    active = equipment.OverclockActive;
                }
            }
            return new NativeHudExtras(showClues, count, goal,
                narrative && narrative.HasMemory(NativeMemoryKind.Private),
                narrative && narrative.HasMemory(NativeMemoryKind.System),
                narrative && narrative.HasMemory(NativeMemoryKind.InitialEcho),
                equipment, gear, hint, active, tabletAvailable);
        }
    }

    // Small native layers beneath the existing ScaleWithScreenSize Canvas.
    [RequireComponent(typeof(RectTransform))]
    public sealed class NativeHudView : MonoBehaviour
    {
        static readonly Color Shadow = new Color32(5, 14, 18, 185);
        static readonly Color Amber = new Color32(255, 193, 83, 255);
        static readonly Color Faint = new Color32(99, 132, 139, 255);
        static readonly Color Bright = new Color32(229, 241, 237, 255);

        [Header("GF02 transparent pixel icons; text and meters remain native")]
        public Sprite healthIconSprite, weaponIconSprite, equipmentIconSprite, tabletIconSprite;
        public Sprite keycapSprite;

        RectTransform healthGroup, objectiveGroup, fireGroup, equipmentGroup, tabletGroup, promptGroup;
        RectTransform healthFill;
        GameObject[] clueRows;
        UnityEngine.UI.Image[] clueMarks, icons, keycaps;
        GameObject[] iconFallbacks;
        TextMeshProUGUI healthValue, objectiveTitle, objectiveProgress, objectiveDetail;
        TextMeshProUGUI[] clueLabels;
        TextMeshProUGUI fireMode, fireAction, elapsedLabel, scoreLabel;
        TextMeshProUGUI equipmentTitle, equipmentHint, tabletTitle, tabletHint, promptLabel;
        GameObject legacyEquipmentPanel;
        bool built;
        float lastHealthRatio = -1f;
        string lastObjectiveDetail;
        string[] clueTexts = new string[0];

        public static NativeHudView Create(Transform canvasParent, TMP_FontAsset chineseFont)
        {
            var oldTitle = canvasParent.Find("Title");
            if (oldTitle) oldTitle.gameObject.SetActive(false);
            var root = FlowUiElements.OverlayRoot("GF02 HUD", canvasParent);
            var view = root.gameObject.AddComponent<NativeHudView>();
            var oldEquipment = canvasParent.Find("Pulse coil status");
            view.legacyEquipmentPanel = oldEquipment ? oldEquipment.gameObject : null;
            view.Build(chineseFont);
            return view;
        }

        public void Build(TMP_FontAsset chineseFont)
        {
            if (built) return;
            built = true;
            var root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            icons = new UnityEngine.UI.Image[4];
            iconFallbacks = new GameObject[4];
            keycaps = new UnityEngine.UI.Image[3];

            healthGroup = Corner("Health", new Vector2(0, 1), new Vector2(0, 1), 300, 66);
            Face(healthGroup, 300, 60);
            icons[0] = Icon("Health Icon", healthGroup, 8, 7, 28, 28, out iconFallbacks[0]);
            Cross(iconFallbacks[0].transform, FlowUiElements.Mint);
            healthValue = Label("Value", healthGroup, 44, 5, 246, 36, chineseFont, 25, Bright);
            var track = FlowUiElements.Box("Health Track", healthGroup, 8, 49, 284, 7);
            FlowUiElements.Image(track, Faint);
            healthFill = FlowUiElements.Fill("Health Fill", track);
            FlowUiElements.Image(healthFill, FlowUiElements.Mint);
            for (int i = 1; i < 10; i++)
                FlowUiElements.Image(FlowUiElements.Box("Tick", healthGroup,
                    8 + i * 28.4f - 2, 49, 2, 7), Shadow);

            objectiveGroup = Corner("Objective", Vector2.one, Vector2.one, 360, 126);
            Face(objectiveGroup, 360, 126);
            objectiveTitle = Label("Title", objectiveGroup, 10, 5, 246, 30, chineseFont, 21, Amber);
            objectiveProgress = Label("Progress", objectiveGroup, 254, 5, 96, 30,
                chineseFont, 19, Bright, TextAlignmentOptions.MidlineRight);
            objectiveDetail = Label("Detail", objectiveGroup, 16, 42, 334, 77,
                chineseFont, 17, Bright);
            clueRows = new GameObject[3];
            clueMarks = new UnityEngine.UI.Image[3];
            clueLabels = new TextMeshProUGUI[3];
            for (int i = 0; i < clueRows.Length; i++)
            {
                var row = FlowUiElements.Box("Memory Clue " + i, objectiveGroup,
                    16, 40 + i * 27, 332, 25);
                clueRows[i] = row.gameObject;
                clueMarks[i] = FlowUiElements.Image(FlowUiElements.Box("Diamond", row,
                    4, 8, 10, 10), Faint);
                clueMarks[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                clueLabels[i] = Label("Clue", row, 27, 0, 302, 25, chineseFont, 17, Bright);
            }

            fireGroup = Corner("Fire and Stats", Vector2.zero, Vector2.zero, 300, 93);
            Face(fireGroup, 300, 93);
            icons[1] = Icon("Weapon Icon", fireGroup, 8, 5, 34, 27, out iconFallbacks[1]);
            Weapon(iconFallbacks[1].transform, Bright);
            fireMode = Label("Fire Mode", fireGroup, 47, 4, 118, 28,
                chineseFont, 18, FlowUiElements.Mint);
            keycaps[0] = Keycap("Space", fireGroup, 169, 5, 64, 25, chineseFont, "SPACE");
            fireAction = Label("Fire Action", fireGroup, 239, 4, 56, 28, chineseFont, 17, Bright);
            elapsedLabel = Label("Elapsed and Kills", fireGroup, 8, 36, 284, 25,
                chineseFont, 16, Bright);
            scoreLabel = Label("Sync and Difference", fireGroup, 8, 63, 284, 24,
                chineseFont, 16, FlowUiElements.Muted);

            equipmentGroup = Corner("Equipment", Vector2.zero, Vector2.zero, 260, 67);
            Face(equipmentGroup, 260, 67);
            icons[2] = Icon("Equipment Icon", equipmentGroup, 8, 13, 32, 32,
                out iconFallbacks[2]);
            Chip(iconFallbacks[2].transform, Amber);
            equipmentTitle = Label("Equipment Status", equipmentGroup, 45, 6, 208, 27,
                chineseFont, 16, Amber);
            equipmentHint = Label("Equipment Action", equipmentGroup, 45, 34, 208, 27,
                chineseFont, 15, Bright);
            equipmentGroup.gameObject.SetActive(false);

            tabletGroup = Corner("Tablet Hint", Vector2.right, Vector2.right, 270, 63);
            Face(tabletGroup, 270, 63);
            icons[3] = Icon("Tablet Icon", tabletGroup, 8, 7, 26, 43, out iconFallbacks[3]);
            Tablet(iconFallbacks[3].transform, FlowUiElements.Mint);
            keycaps[1] = Keycap("Tab", tabletGroup, 43, 7, 48, 25, chineseFont, "TAB");
            tabletTitle = Label("Tablet Action", tabletGroup, 97, 6, 166, 27,
                chineseFont, 18, Bright);
            tabletHint = Label("Local Communication", tabletGroup, 43, 35, 218, 23,
                chineseFont, 15, FlowUiElements.Mint);

            promptGroup = Corner("Interaction", new Vector2(.5f, 0),
                new Vector2(.5f, 0), 430, 43);
            Face(promptGroup, 430, 43);
            keycaps[2] = Keycap("Interact", promptGroup, 7, 6, 31, 30, chineseFont, "E");
            promptLabel = Label("Action", promptGroup, 47, 3, 374, 36,
                chineseFont, 17, Bright);
            promptGroup.gameObject.SetActive(false);

            ApplyArt();
            AdaptLayout();
        }

        public void ApplyArt()
        {
            if (icons == null) return;
            Sprite[] sprites = { healthIconSprite, weaponIconSprite, equipmentIconSprite,
                tabletIconSprite };
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].sprite = sprites[i];
                icons[i].color = sprites[i] ? Color.white : Color.clear;
                icons[i].preserveAspect = true;
                iconFallbacks[i].SetActive(!sprites[i]);
            }
            for (int i = 0; i < keycaps.Length; i++)
            {
                keycaps[i].sprite = keycapSprite;
                keycaps[i].type = keycapSprite && keycapSprite.border.sqrMagnitude > 0
                    ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
                keycaps[i].color = keycapSprite ? Color.white : new Color32(42, 58, 64, 230);
            }
        }

        public void Bind(NativeHudDisplay state)
        {
            if (!built) return;
            SetText(healthValue, Mathf.Max(0, Mathf.CeilToInt(state.Health)) + " / " +
                Mathf.Max(0, Mathf.CeilToInt(state.MaxHealth)));
            float ratio = state.MaxHealth > 0 ? Mathf.Clamp01(state.Health / state.MaxHealth) : 0;
            if (!Mathf.Approximately(ratio, lastHealthRatio))
            {
                healthFill.anchorMax = new Vector2(ratio, 1);
                healthFill.offsetMin = healthFill.offsetMax = Vector2.zero;
                lastHealthRatio = ratio;
            }

            string objective = state.Objective ?? string.Empty;
            int breakAt = objective.IndexOf('\n');
            string headline = breakAt < 0 ? objective : objective.Substring(0, breakAt);
            string detail = breakAt < 0 ? string.Empty : objective.Substring(breakAt + 1);
            int slash = headline.IndexOf(" / ", StringComparison.Ordinal);
            if (slash >= 0 && slash <= 3) headline = headline.Substring(slash + 3);
            int progressAt = headline.IndexOf("   ", StringComparison.Ordinal);
            if (progressAt >= 0) headline = headline.Substring(0, progressAt);
            SetText(objectiveTitle, headline);
            if (detail != lastObjectiveDetail)
            {
                clueTexts = detail.Split(new[] { '。' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < clueRows.Length && i < clueTexts.Length; i++)
                    SetText(clueLabels[i], clueTexts[i].Trim().Replace("：", " · "));
                lastObjectiveDetail = detail;
            }
            bool clues = state.Extras.HasState && state.Extras.ShowMemoryClues &&
                clueTexts.Length >= clueRows.Length;
            SetVisible(objectiveProgress.gameObject, clues);
            if (clues)
                SetText(objectiveProgress, state.Extras.MemoryCount + " / " + state.Extras.MemoryGoal);
            SetVisible(objectiveDetail.gameObject, !clues);
            if (!clues) SetText(objectiveDetail, detail);
            for (int i = 0; i < clueRows.Length; i++)
            {
                SetVisible(clueRows[i], clues);
                if (clues)
                {
                    bool found = i == 0 ? state.Extras.PrivateMemory :
                        i == 1 ? state.Extras.SystemMemory : state.Extras.InitialEcho;
                    clueMarks[i].color = found ? FlowUiElements.Mint : Bright;
                    clueLabels[i].color = found ? FlowUiElements.Mint : Bright;
                }
            }

            SetText(fireMode, state.AutoFire ? "自动开火" : "停火");
            SetText(fireAction, state.AutoFire ? "停火" : "开火");
            fireMode.color = state.AutoFire ? FlowUiElements.Mint : Amber;
            int seconds = Mathf.Max(0, Mathf.FloorToInt(state.ElapsedSeconds));
            SetText(elapsedLabel, string.Format("{0:00}:{1:00}  |  击败 {2}",
                seconds / 60, seconds % 60, Mathf.Max(0, state.Kills)));
            SetText(scoreLabel, "同步 " + state.Sync + "  |  差异 " + state.Difference);

            bool hasGear = state.Extras.HasState && state.Extras.HasEquipmentSource;
            SetVisible(equipmentGroup.gameObject, hasGear);
            if (hasGear)
            {
                SetText(equipmentTitle, state.Extras.EquipmentTitle);
                SetText(equipmentHint, state.Extras.EquipmentHint);
                equipmentHint.color = state.Extras.EquipmentActive ? Amber : Bright;
                if (legacyEquipmentPanel && legacyEquipmentPanel.activeSelf)
                    legacyEquipmentPanel.SetActive(false);
            }
            bool tabletAvailable = !state.Extras.HasState || state.Extras.TabletAvailable;
            SetText(tabletTitle, tabletAvailable ? "战术平板" : "平板暂不可用");
            SetText(tabletHint, tabletAvailable ? "安全通讯 / 本地" : "当前不可进入");
            tabletHint.color = tabletAvailable ? FlowUiElements.Mint : FlowUiElements.Muted;
            bool hasPrompt = !string.IsNullOrWhiteSpace(state.InteractionPrompt);
            SetVisible(promptGroup.gameObject, hasPrompt);
            if (hasPrompt) SetText(promptLabel, state.InteractionPrompt);
        }

        public void Show(bool visible) => gameObject.SetActive(visible);

        void OnRectTransformDimensionsChange()
        {
            if (built) AdaptLayout();
        }

        void AdaptLayout()
        {
            if (!healthGroup || !objectiveGroup || !fireGroup || !tabletGroup) return;
            var rect = GetComponent<RectTransform>().rect;
            if (rect.width <= 0 || rect.height <= 0) return;
            float scale = Mathf.Clamp(Mathf.Min(rect.width / 1050f, rect.height / 640f), .62f, 1f);
            float margin = Mathf.Max(14f, Mathf.Min(24f, rect.width * .025f));
            healthGroup.localScale = objectiveGroup.localScale = fireGroup.localScale =
                equipmentGroup.localScale = tabletGroup.localScale = promptGroup.localScale =
                Vector3.one * scale;
            healthGroup.anchoredPosition = new Vector2(margin, -margin);
            objectiveGroup.anchoredPosition = new Vector2(-margin, -margin);
            fireGroup.anchoredPosition = new Vector2(margin, margin);
            equipmentGroup.anchoredPosition = new Vector2(margin + 318f * scale, margin);
            tabletGroup.anchoredPosition = new Vector2(-margin, margin);
            promptGroup.anchoredPosition = new Vector2(0, margin + 108f * scale);
        }

        RectTransform Corner(string name, Vector2 anchor, Vector2 pivot, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        static void Face(Transform parent, float width, float height)
        {
            FlowUiElements.Image(FlowUiElements.Box("Local Contrast", parent, 0, 0,
                width, height), Shadow);
        }

        static TextMeshProUGUI Label(string name, Transform parent, float x, float y,
            float width, float height, TMP_FontAsset font, int size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var label = FlowUiElements.Label(FlowUiElements.Box(name, parent, x, y,
                width, height), font, string.Empty, size, color, alignment);
            label.overflowMode = TextOverflowModes.Truncate;
            return label;
        }

        static UnityEngine.UI.Image Icon(string name, Transform parent, float x, float y,
            float width, float height, out GameObject fallback)
        {
            var rect = FlowUiElements.Box(name, parent, x, y, width, height);
            var image = FlowUiElements.Image(rect, Color.clear);
            fallback = FlowUiElements.Fill("Native Fallback", rect).gameObject;
            return image;
        }

        static void Block(Transform parent, string name, float x, float y, float width,
            float height, Color color)
        {
            FlowUiElements.Image(FlowUiElements.Box(name, parent, x, y, width, height), color);
        }

        static void Cross(Transform parent, Color color)
        {
            Block(parent, "Vertical", 11, 2, 6, 24, color);
            Block(parent, "Horizontal", 2, 11, 24, 6, color);
        }

        static void Weapon(Transform parent, Color color)
        {
            Block(parent, "Barrel", 3, 8, 27, 5, color);
            Block(parent, "Body", 7, 13, 19, 6, color);
            Block(parent, "Grip", 14, 18, 6, 8, color);
        }

        static void Chip(Transform parent, Color color)
        {
            Block(parent, "Top", 5, 4, 22, 3, color);
            Block(parent, "Bottom", 5, 25, 22, 3, color);
            Block(parent, "Left", 5, 7, 3, 18, color);
            Block(parent, "Right", 24, 7, 3, 18, color);
            Block(parent, "Core", 13, 12, 6, 8, color);
        }

        static void Tablet(Transform parent, Color color)
        {
            Block(parent, "Top", 3, 2, 20, 3, color);
            Block(parent, "Bottom", 3, 38, 20, 3, color);
            Block(parent, "Left", 3, 5, 3, 33, color);
            Block(parent, "Right", 20, 5, 3, 33, color);
            Block(parent, "Screen", 8, 28, 10, 6, color);
        }

        static UnityEngine.UI.Image Keycap(string name, Transform parent, float x, float y,
            float width, float height, TMP_FontAsset font, string value)
        {
            var rect = FlowUiElements.Box(name, parent, x, y, width, height);
            var image = FlowUiElements.Image(rect, new Color32(42, 58, 64, 230));
            FlowUiElements.Label(FlowUiElements.Fill("Key", rect), font, value, 15,
                Bright, TextAlignmentOptions.Center).overflowMode = TextOverflowModes.Truncate;
            return image;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            value = value ?? string.Empty;
            if (label.text != value) label.text = value;
        }

        static void SetVisible(GameObject target, bool visible)
        {
            if (target.activeSelf != visible) target.SetActive(visible);
        }
    }
}
