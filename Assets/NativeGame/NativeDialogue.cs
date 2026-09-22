using UnityEngine;
using TMPro;
namespace Echo.NativeGame
{
    public sealed class NativeDialogue : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text message;
        public UnityEngine.UI.Button closeButton;
        public bool IsOpen { get; private set; }
        public int SessionNumber { get; private set; }
        void Awake() { panel.SetActive(false); closeButton.onClick.AddListener(Close); }
        public void Show(string text)
        { SessionNumber++; message.text = text; IsOpen = true; panel.SetActive(true); }
        public void Close()
        { IsOpen = false; if (panel) panel.SetActive(false); if (UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null); }
        void OnDisable() { Close(); }
    }
}
