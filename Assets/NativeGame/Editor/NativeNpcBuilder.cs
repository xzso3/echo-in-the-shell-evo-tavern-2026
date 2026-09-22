using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Echo.NativeGame.Editor
{
    public static class NativeNpcBuilder
    {
        const string PrefabPath = "Assets/NativeGame/Prefabs/LocalArchivist.prefab";
        [MenuItem("Echo/Native/Connect Local NPC Branches")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (run.rules.GetComponent<NativeNpcEcaRules>() || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) throw new InvalidOperationException("Local NPC already authored.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NativeFontSetup.FontPath);
            var actor = new GameObject("Local archivist").AddComponent<NativeLocalNpc>();
            actor.interaction = actor.GetComponent<NativeInteraction>(); actor.interaction.npc = actor;
            actor.interaction.promptOverride = "E / 与本地档案员交谈";
            var visual = new GameObject("Static NPC visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(actor.transform, false); visual.transform.localPosition = new Vector3(0, -.2f, 0);
            visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberCity/Sprites/Cyber operative.asset");
            visual.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CyberCity/Materials/Actors.mat");
            visual.color = new Color(1, .78f, .46f); visual.sortingOrder = 100;
            actor.interaction.indicator = visual; actor.gameObject.AddComponent<CircleCollider2D>().radius = .35f;
            var label = new GameObject("NPC local identity").AddComponent<TextMeshPro>(); label.transform.SetParent(actor.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(0, 1.3f); label.rectTransform.sizeDelta = new Vector2(8, 1);
            label.font = font; label.fontSharedMaterial = font.material; label.fontSize = 4.8f; label.alignment = TextAlignmentOptions.Center;
            label.text = "本地档案员 / 可选委托"; label.color = new Color(1, .84f, .55f); label.GetComponent<MeshRenderer>().sortingOrder = 200;
            var prefab = PrefabUtility.SaveAsPrefabAsset(actor.gameObject, PrefabPath); UnityEngine.Object.DestroyImmediate(actor.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, GameObject.Find("Actors").transform); instance.transform.position = new Vector2(-11, 3);
            var npc = instance.GetComponent<NativeLocalNpc>(); npc.interaction.run = run; npc.interaction.map = run.map;
            PrefabUtility.RecordPrefabInstancePropertyModifications(npc.interaction);
            run.hud.interactables = run.hud.interactables.Concat(new[] { npc.interaction }).ToArray();
            var rules = run.rules.gameObject.AddComponent<NativeNpcEcaRules>(); rules.level = run; rules.npc = npc; rules.quest = run.quest; rules.narrative = run.narrative; rules.dialogue = run.dialogue;
            var choices = new GameObject("NPC branch choices", typeof(RectTransform)).GetComponent<RectTransform>(); choices.SetParent(run.dialogue.panel.transform, false);
            choices.anchorMin = Vector2.zero; choices.anchorMax = Vector2.one; choices.offsetMin = choices.offsetMax = Vector2.zero;
            run.dialogue.choicesRoot = choices.gameObject;
            run.dialogue.privateChoiceButton = Choice("Private record choice", "选择 A：个人记录", choices, 118, font);
            run.dialogue.routeChoiceButton = Choice("Route report choice", "选择 B：路线报告", choices, 70, font);
            run.dialogue.normalHeight = 310; run.dialogue.choiceHeight = 430; choices.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            NativeFontSetup.Apply();
            Debug.Log("NATIVE_NPC_BUILD: local NPC at (-11,3); Interaction -> NPC ECA -> Quest, Dialogue choices, phone status; existing main path retained.");
        }
        static Button Choice(string name, string caption, Transform parent, float y, TMP_FontAsset font)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); image.transform.SetParent(parent, false);
            var rect = image.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(732, 40); image.color = new Color(.16f, .94f, .83f);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); label.transform.SetParent(image.transform, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = font; label.fontSharedMaterial = font.material; label.fontSize = 24; label.text = caption; label.color = new Color(.025f, .048f, .065f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return button;
        }
    }
}
