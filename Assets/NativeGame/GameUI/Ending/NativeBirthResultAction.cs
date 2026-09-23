using System;
using Echo.NativeGame.PhoneUI;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.GameUI
{
    // Adds one native hit target beside the existing physical return key. The
    // approved seven tablet sprites and the tablet's pages remain untouched.
    [RequireComponent(typeof(RectTransform))]
    public sealed class NativeBirthResultAction : MonoBehaviour
    {
        TMP_Text physicalReturnLabel;
        UnityEngine.UI.Button resultsButton;
        bool built;

        public event Action ViewResultsRequested;

        // Call only after CommanderTabletView.Build has created "Tactical Tablet".
        public static NativeBirthResultAction Create(CommanderTabletView tabletView,
            TMP_FontAsset chineseFont)
        {
            if (!tabletView) return null;
            var tablet = tabletView.transform.Find("Tactical Tablet");
            if (!tablet) return null;
            var existing = tablet.Find("GF01 Birth Result");
            if (existing) return existing.GetComponent<NativeBirthResultAction>();
            var rect = FlowUiElements.Box("GF01 Birth Result", tablet, 580, 527, 216, 48);
            var view = rect.gameObject.AddComponent<NativeBirthResultAction>();
            view.Build(chineseFont, tablet);
            return view;
        }

        public void Build(TMP_FontAsset chineseFont, Transform tablet)
        {
            if (built) return;
            built = true;
            resultsButton = FlowUiElements.Button(GetComponent<RectTransform>(), chineseFont,
                "查看本局结果", FlowUiElements.Mint, FlowUiElements.Ink, 18);
            resultsButton.onClick.AddListener(() => ViewResultsRequested?.Invoke());
            var returnKey = tablet ? tablet.Find("Physical Return/Return Label") : null;
            physicalReturnLabel = returnKey ? returnKey.GetComponent<TMP_Text>() : null;
            gameObject.SetActive(false);
        }

        public void Show(bool visible)
        {
            if (physicalReturnLabel) physicalReturnLabel.text = visible ? "查看本局结果" : "返回";
            gameObject.SetActive(visible);
        }

        void OnDisable()
        {
            if (physicalReturnLabel) physicalReturnLabel.text = "返回";
        }
    }
}
