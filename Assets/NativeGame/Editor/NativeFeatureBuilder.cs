using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Echo.NativeGame.Editor
{
    public static class NativeFeatureBuilder
    {
        static TMP_FontAsset font;
        static Color Cyan => new Color(.16f, .94f, .83f);
        static Color Dark => new Color(.025f, .048f, .065f, .98f);
        [MenuItem("Echo/Native/Connect U4 Local Features")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (run.hud.phone) throw new InvalidOperationException("Local features already connected.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NativeGame/Fonts/FusionPixel/FusionPixel12 Bitmap.asset");
            var ui = run.hud.transform;
            UnityEngine.Object.DestroyImmediate(run.hud.phonePanel);
            var sequence = ui.gameObject.AddComponent<NativeEndingSequence>(); sequence.level = run; run.endingSequence = sequence;
            var overlay = Box("Birth - white world then black", ui, Vector2.zero, Vector2.zero, Vector2.zero, Color.white);
            overlay.rectTransform.anchorMin = Vector2.zero; overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = overlay.rectTransform.offsetMax = Vector2.zero;
            sequence.overlay = overlay.gameObject; sequence.screen = overlay;
            sequence.fist = Box("Shell fist", overlay.transform, new Vector2(.5f,.5f), new Vector2(-220,-120), new Vector2(140,80), new Color(.06f,.09f,.12f));
            // A simple diagonal fracture, deliberately concrete Canvas geometry rather than an animation framework.
            sequence.fracture = Box("First punch fracture", overlay.transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(7,280), new Color(.08f,.12f,.14f));
            sequence.fracture.transform.localRotation = Quaternion.Euler(0,0,-32);
            sequence.caption = Text("First punch prompt", overlay.transform, new Vector2(.5f,.5f), new Vector2(0,150), new Vector2(800,140), 30);
            sequence.caption.alignment = TextAlignmentOptions.Center; overlay.gameObject.SetActive(false);
            var panel = Box("Phone - three local pages", ui, new Vector2(1,.5f), new Vector2(-24,-12), new Vector2(540,500), Dark);
            run.hud.phonePanel = panel.gameObject;
            var phone = ui.gameObject.AddComponent<NativePhone>(); phone.level = run; run.hud.phone = phone;
            phone.heading = Text("Phone title", panel.transform, new Vector2(0,1), new Vector2(18,-14), new Vector2(504,30), 23); phone.heading.color = Cyan;
            phone.tabs = new Button[3];
            for (int i=0;i<3;i++) phone.tabs[i] = Button(NativePhone.PageLabel((NativePhone.Page)i), panel.transform, new Vector2(18+i*170,-54), new Vector2(164,34), true);
            var viewport = Box("Scrollable page", panel.transform, new Vector2(0,1), new Vector2(18,-104), new Vector2(504,190), new Color(0,0,0,.12f));
            viewport.gameObject.AddComponent<RectMask2D>();
            phone.body = Text("Page content", viewport.transform, new Vector2(0,1), Vector2.zero, new Vector2(492,190), 16);
            phone.body.enableWordWrapping = true;
            phone.scroll = viewport.gameObject.AddComponent<ScrollRect>(); phone.scroll.viewport = viewport.rectTransform; phone.scroll.content = phone.body.rectTransform;
            phone.scroll.horizontal = false; phone.scroll.vertical = true; phone.scroll.movementType = ScrollRect.MovementType.Clamped; phone.scroll.scrollSensitivity = 24;
            phone.actions = new Button[8];
            for (int i=0;i<8;i++) phone.actions[i] = Button("操作 " + i, panel.transform, new Vector2(18+(i%2)*254,58+(3-i/2)*36), new Vector2(248,32));
            run.hud.phoneCloseButton = Button("关闭 / Tab", panel.transform, new Vector2(18,14), new Vector2(248,32));
            phone.restart = Button("重新开始", panel.transform, new Vector2(272,14), new Vector2(248,32));
            run.hud.phoneArchive = null;
            panel.gameObject.SetActive(false);
            run.rules.exit.promptOverride = "E / 选择写入或销毁";
            run.quest.completedObjective = "05 / 最终节点\n前往东侧档案节点，按 E 作出最后的选择。";
            run.rules.terminalMessage = "指挥官 / 三段记忆都保留下来了。\n东侧通路已开启。支援能帮上忙，但请先看清授权范围。按 Tab 打开手机。前方机体仍为测试形象，正式身份尚未确定。";
            PrefabUtility.RecordPrefabInstancePropertyModifications(run);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("U4 local phone and narrative ending sequence connected; actual Support component awaits owner integration.");
        }
        [MenuItem("Echo/Native/Connect Real Support")]
        public static void ConnectSupport()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            var run = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (!run.hud.phone || !run.endingSequence || !run.endingSequence.overlay) throw new InvalidOperationException("U4 saved scene is incomplete.");
            if (run.rules.supportComponent) throw new InvalidOperationException("Support already connected.");
            var support = new GameObject("Support - local confirmed effects").AddComponent<NativeSupportController>();
            support.level = run; support.boss = run.rules.bossComponent as NativeBossController;
            run.rules.supportComponent = support;
            EditorUtility.SetDirty(run.rules); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("U4 scene references reloaded; saving real Support integration.");
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("U4 real Support connected: UI requests, confirmed effects, Authorized -> ECA -> Narrative.");
        }
        static Image Box(string name, Transform parent, Vector2 anchor, Vector2 at, Vector2 size, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); image.transform.SetParent(parent,false);
            var rect = image.rectTransform; rect.anchorMin=rect.anchorMax=anchor; rect.pivot=anchor; rect.anchoredPosition=at; rect.sizeDelta=size; image.color=color; return image;
        }
        static TMP_Text Text(string name, Transform parent, Vector2 anchor, Vector2 at, Vector2 size, float fontSize)
        {
            var label = new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); label.transform.SetParent(parent,false);
            var rect=label.rectTransform; rect.anchorMin=rect.anchorMax=anchor; rect.pivot=anchor; rect.anchoredPosition=at; rect.sizeDelta=size;
            label.font=font; label.fontSize=fontSize; label.color=Color.white; label.raycastTarget=false; return label;
        }
        static Button Button(string label, Transform parent, Vector2 at, Vector2 size, bool top=false)
        {
            var image=Box(label,parent,top?new Vector2(0,1):Vector2.zero,at,size,Cyan);
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image; button.navigation=new Navigation { mode=Navigation.Mode.None };
            var text=Text("Label",image.transform,new Vector2(.5f,.5f),Vector2.zero,size,16); text.text=label; text.color=Dark; text.alignment=TextAlignmentOptions.Center; return button;
        }
    }
}
