using System;
using UnityEngine;
using TMPro;
namespace Echo.NativeGame
{
    public enum NativeDialogueChoice { PrivateRecord, RouteReport }
    public sealed class NativeDialogue : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text message;
        public UnityEngine.UI.Button closeButton;
        public GameObject choicesRoot;
        public UnityEngine.UI.Button privateChoiceButton, routeChoiceButton;
        public float normalHeight = 310, choiceHeight = 430;
        public bool IsOpen { get; private set; }
        public bool HasChoices { get; private set; }
        public UnityEngine.Object ChoiceContext { get; private set; }
        public int SessionNumber { get; private set; }
        public event Action<UnityEngine.Object, NativeDialogueChoice> ChoiceSelected;
        void Awake()
        {
            panel.SetActive(false); closeButton.onClick.AddListener(Close); ClearChoices();
            if (privateChoiceButton) privateChoiceButton.onClick.AddListener(() => SelectChoice(NativeDialogueChoice.PrivateRecord));
            if (routeChoiceButton) routeChoiceButton.onClick.AddListener(() => SelectChoice(NativeDialogueChoice.RouteReport));
        }
        public void Show(string text)
        { ClearChoices(); SessionNumber++; message.text = text; IsOpen = true; panel.SetActive(true); }
        public void ShowChoices(UnityEngine.Object context, string text, string privateLabel, string routeLabel)
        {
            Show(text); ChoiceContext = context; HasChoices = true;
            privateChoiceButton.GetComponentInChildren<TMP_Text>(true).text = privateLabel;
            routeChoiceButton.GetComponentInChildren<TMP_Text>(true).text = routeLabel;
            choicesRoot.SetActive(true); SetHeight(choiceHeight);
        }
        public void SelectChoice(NativeDialogueChoice choice)
        {
            if (!IsOpen || !HasChoices || (choice != NativeDialogueChoice.PrivateRecord && choice != NativeDialogueChoice.RouteReport)) return;
            var context = ChoiceContext;
            ClearChoices(); // Consume this choice display before dispatch; never close the listener's new reply.
            ChoiceSelected?.Invoke(context, choice);
        }
        void SetHeight(float height)
        { if (panel) { var rect = panel.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(rect.sizeDelta.x, height); } }
        void ClearChoices()
        { HasChoices = false; ChoiceContext = null; if (choicesRoot) choicesRoot.SetActive(false); SetHeight(normalHeight); }
        public void Close()
        { IsOpen = false; ClearChoices(); if (panel) panel.SetActive(false); if (UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null); }
        void OnDisable() { Close(); }
    }
}
