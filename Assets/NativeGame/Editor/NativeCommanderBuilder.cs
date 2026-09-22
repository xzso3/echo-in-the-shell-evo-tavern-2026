using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.NativeGame.Editor
{
    public static class NativeCommanderBuilder
    {
        [MenuItem("Echo/Native/Connect P2 Commander Text")]
        public static void Connect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.OpenScene(NativeDemoBuilder.ScenePath);
            var level = UnityEngine.Object.FindObjectOfType<NativeRunController>();
            if (!level || !level.hud || !level.hud.phone) throw new InvalidOperationException("Saved U4 phone is missing.");
            var phone = level.hud.phone;
            if (phone.composer || phone.proxy || phone.safeNode) throw new InvalidOperationException("Commander input already connected.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NativeFontSetup.FontPath);
            if (!font) throw new InvalidOperationException("FusionPixel font is missing.");

            var nodeObject = new GameObject("Safe commander text node");
            nodeObject.transform.SetParent(GameObject.Find("World").transform, false);
            nodeObject.transform.position = new Vector3(-13, 3, 0);
            var node = nodeObject.AddComponent<NativeCommanderSafeNode>(); node.level = level;
            var plate = new GameObject("Cyan terminal plate").AddComponent<SpriteRenderer>();
            plate.transform.SetParent(nodeObject.transform, false);
            plate.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CyberCity/Sprites/Maintenance plate.asset");
            plate.color = new Color(.18f, .96f, .88f, 1); plate.sortingOrder = 90;
            plate.transform.localScale = new Vector3(1.4f, 1.4f, 1);
            var label = new GameObject("Safe text node label").AddComponent<TextMeshPro>();
            label.transform.SetParent(nodeObject.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(0, -1.1f);
            label.rectTransform.sizeDelta = new Vector2(7, 1);
            label.font = font; label.fontSharedMaterial = font.material;
            label.fontSize = 4.5f; label.alignment = TextAlignmentOptions.Center;
            label.text = "安全通讯 / Tab"; label.color = new Color(.25f, 1, .89f);
            label.GetComponent<MeshRenderer>().sortingOrder = 200;
            phone.safeNode = node;
            phone.proxy = phone.gameObject.AddComponent<NativeCommanderProxy>();

            var panel = level.hud.phonePanel.transform;
            var inputImage = new GameObject("Commander draft", typeof(RectTransform), typeof(Image), typeof(TMP_InputField)).GetComponent<Image>();
            inputImage.transform.SetParent(panel, false);
            Place(inputImage.rectTransform, new Vector2(18, 207), new Vector2(382, 38));
            inputImage.color = new Color(.08f, .15f, .18f, 1);
            var field = inputImage.GetComponent<TMP_InputField>();
            field.targetGraphic = inputImage;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 300;
            field.selectionColor = new Color(.16f, .94f, .83f, .45f);
            var viewport = new GameObject("Draft viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(inputImage.transform, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8, 3); viewport.offsetMax = new Vector2(-8, -3);
            var content = Label("Draft text", viewport, font, "", new Color(1, 1, 1));
            var placeholder = Label("Draft placeholder", viewport, font, "在安全节点输入消息（最多300字）", new Color(.57f, .72f, .73f));
            field.textViewport = viewport; field.textComponent = content; field.placeholder = placeholder;
            phone.composer = field;

            var sendImage = new GameObject("Send commander text", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Image>();
            sendImage.transform.SetParent(panel, false);
            Place(sendImage.rectTransform, new Vector2(406, 207), new Vector2(114, 38));
            sendImage.color = new Color(.16f, .94f, .83f);
            var send = sendImage.GetComponent<Button>(); send.targetGraphic = sendImage;
            send.navigation = new Navigation { mode = Navigation.Mode.None };
            var caption = Label("Send label", sendImage.transform, font, "发送", new Color(.025f, .048f, .065f));
            caption.alignment = TextAlignmentOptions.Center;
            phone.sendButton = send;
            phone.body.richText = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P2_COMMANDER_CONNECTED: safe text node (-13,3), 300-character draft, text-only proxy transport; URL intentionally empty.");
        }

        static void Place(RectTransform rect, Vector2 point, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = point; rect.sizeDelta = size;
        }
        static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string value, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = font; text.fontSharedMaterial = font.material; text.fontSize = 16;
            text.text = value; text.color = color; text.raycastTarget = false;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            return text;
        }
    }
}
