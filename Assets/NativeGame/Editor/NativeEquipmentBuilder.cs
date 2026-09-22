using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.NativeGame.Editor
{
    // Run only after the scene owner has finished its Map/Combat edits and owns the Unity Editor slot.
    public static class NativeEquipmentBuilder
    {
        const string EquipmentPrefabPath = "Assets/NativeGame/Prefabs/PulseCoilEquipment.prefab";
        const string PickupPrefabPath = "Assets/NativeGame/Prefabs/PulseCoilPickup.prefab";
        const string FontPath = "Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset";

        [MenuItem("Echo/Native/Connect Pulse Coil Equipment")]
        public static void Connect() { ConnectAt(new Vector2(-7, -2)); }

        // The integrator can choose a reachable position in the finished P2 map.
        public static void ConnectAt(Vector2 pickupPosition)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (!run || !run.combat || !run.hud || !run.player ||
                UnityEngine.Object.FindObjectOfType<NativeEquipment>())
                throw new InvalidOperationException("NativeDemo needs one connected run, and equipment must not already exist.");
            var actors = GameObject.Find("Actors");
            if (!actors) throw new InvalidOperationException("Actors root is missing.");
            var equipmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EquipmentPrefabPath);
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (!equipmentPrefab || !pickupPrefab || !font) throw new InvalidOperationException("Equipment prefab, pickup prefab or Chinese font is missing.");

            var equipment = ((GameObject)PrefabUtility.InstantiatePrefab(equipmentPrefab)).GetComponent<NativeEquipment>();
            equipment.run = run;
            PrefabUtility.RecordPrefabInstancePropertyModifications(equipment);
            var pickup = ((GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, actors.transform)).GetComponent<NativeEquipmentPickup>();
            pickup.transform.position = pickupPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(pickup.transform);
            pickup.equipment = equipment;
            var interaction = pickup.GetComponent<NativeInteraction>();
            interaction.run = run;
            interaction.map = run.map;
            PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            run.hud.interactables = run.hud.interactables.Concat(new[] { interaction }).ToArray();
            EditorUtility.SetDirty(run.hud);
            AddStatusPanel(run.hud.transform, equipment, font);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P2-05 pulse coil connected: E pickup, F equip/unequip (+12 damage), Q overclock (8 seconds, shot interval x0.6). Position: " + pickupPosition);
        }

        static void AddStatusPanel(Transform canvas, NativeEquipment equipment, TMP_FontAsset font)
        {
            var panel = new GameObject("Pulse coil status", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvas, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-16, -128);
            rect.sizeDelta = new Vector2(390, 78);
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(.025f, .048f, .065f, .9f);
            background.raycastTarget = false;
            var view = panel.AddComponent<NativeEquipmentHud>();
            view.equipment = equipment;
            view.gearLabel = Label("Equipment", panel.transform, font, -8);
            view.effectLabel = Label("Effect", panel.transform, font, -43);
        }

        static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float top)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(16, top);
            rect.sizeDelta = new Vector2(358, 30);
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.fontSize = 18;
            label.color = new Color(.46f, .96f, .86f);
            label.raycastTarget = false;
            return label;
        }
    }
}
