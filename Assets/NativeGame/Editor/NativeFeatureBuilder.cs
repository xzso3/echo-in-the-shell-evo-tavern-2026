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
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
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
            for (int i=0;i<3;i++) phone.tabs[i] = Button(((NativePhone.Page)i).ToString().ToUpperInvariant(), panel.transform, new Vector2(18+i*170,-54), new Vector2(164,34), true);
            var viewport = Box("Scrollable page", panel.transform, new Vector2(0,1), new Vector2(18,-104), new Vector2(504,190), new Color(0,0,0,.12f));
            viewport.gameObject.AddComponent<RectMask2D>();
            phone.body = Text("Page content", viewport.transform, new Vector2(0,1), Vector2.zero, new Vector2(492,190), 16);
            phone.body.enableWordWrapping = true;
            phone.scroll = viewport.gameObject.AddComponent<ScrollRect>(); phone.scroll.viewport = viewport.rectTransform; phone.scroll.content = phone.body.rectTransform;
            phone.scroll.horizontal = false; phone.scroll.vertical = true; phone.scroll.movementType = ScrollRect.MovementType.Clamped; phone.scroll.scrollSensitivity = 24;
            phone.actions = new Button[8];
            for (int i=0;i<8;i++) phone.actions[i] = Button("ACTION " + i, panel.transform, new Vector2(18+(i%2)*254,58+(3-i/2)*36), new Vector2(248,32));
            run.hud.phoneCloseButton = Button("CLOSE / TAB", panel.transform, new Vector2(18,14), new Vector2(248,32));
            phone.restart = Button("RESTART RUN", panel.transform, new Vector2(272,14), new Vector2(248,32));
            run.hud.phoneArchive = null;
            panel.gameObject.SetActive(false);
            run.rules.exit.promptOverride = "E / CHOOSE DESTROY OR UPLOAD";
            run.quest.completedObjective = "05 / FINAL NODE\nReach the eastern archive. E opens the final choice.";
            run.rules.terminalMessage = "COMMANDER / Three sources, none erased.\nThe eastern passage is open. Support can help, but review its authorization first. Tab opens your local phone. The combat shell ahead remains a development placeholder.";
            PrefabUtility.RecordPrefabInstancePropertyModifications(run);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("U4 local phone and narrative ending sequence connected; actual Support component awaits owner integration.");
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
