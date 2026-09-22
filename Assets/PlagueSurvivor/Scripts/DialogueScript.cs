using System;
using TMPro;
using UnityEngine;

namespace PlagueSurvivor
{
    [CreateAssetMenu(menuName = "Neural Lockdown/Dialogue Script")]
    public sealed class DialogueScript : ScriptableObject
    {
        [Serializable]
        public sealed class Choice
        {
            public string text;
            [Tooltip("Node index, or -1 to end the conversation.")]
            public int nextNode = -1;
        }
        [Serializable]
        public sealed class Node
        {
            public string speaker = "District Contact";
            public Sprite portrait;
            [TextArea(2, 6)] public string text;
            [Tooltip("Used only when there are no choices. -1 ends the conversation.")]
            public int nextNode = -1;
            public Choice[] choices = new Choice[0];
        }
        [Header("Presentation")]
        public Sprite panelImage;
        public Sprite optionImage;
        public Sprite defaultPortrait;
        [Tooltip("Optional. Falls back to the existing HUD font.")]
        public TMP_FontAsset font;
        [Header("Script")]
        public string title = "DISTRICT 07 / INCOMING TRANSMISSION";
        public int entryNode;
        public Node[] nodes = new Node[0];

        public bool Validate(out string error)
        {
            error = null;
            if (nodes == null || nodes.Length == 0 || entryNode < 0 || entryNode >= nodes.Length)
                error = "Dialogue requires nodes and a valid entry node.";
            else for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.text)) { error = "Node " + i + " has no text."; break; }
                if (node.choices != null && node.choices.Length > 0)
                {
                    if (node.choices.Length > 4) { error = "Node " + i + " exceeds four choices."; break; }
                    foreach (var choice in node.choices)
                        if (choice == null || string.IsNullOrWhiteSpace(choice.text) || !ValidTarget(choice.nextNode))
                        { error = "Node " + i + " has an invalid choice or target."; break; }
                    if (error != null) break;
                }
                else if (!ValidTarget(node.nextNode)) { error = "Node " + i + " has an invalid next node."; break; }
            }
            return error == null;
        }
        bool ValidTarget(int target) { return target >= -1 && target < nodes.Length; }
    }

    // Keeps branching independent from the UI and from the battle simulation.
    public sealed class DialogueSession
    {
        public DialogueScript Script { get; private set; }
        public int NodeIndex { get; private set; } = -1;
        public bool IsOpen { get { return Script && NodeIndex >= 0; } }
        public DialogueScript.Node Current { get { return IsOpen ? Script.nodes[NodeIndex] : null; } }
        public bool Begin(DialogueScript script, out string error)
        {
            error = "Dialogue script is missing.";
            if (!script || !script.Validate(out error)) return false;
            Script = script; NodeIndex = script.entryNode; return true;
        }
        public bool Choose(int index)
        {
            if (!IsOpen || Current.choices == null || index < 0 || index >= Current.choices.Length) return false;
            MoveTo(Current.choices[index].nextNode); return true;
        }
        public bool Advance()
        {
            if (!IsOpen || (Current.choices != null && Current.choices.Length > 0)) return false;
            MoveTo(Current.nextNode); return true;
        }
        void MoveTo(int index) { if (index < 0) Close(); else NodeIndex = index; }
        public void Close() { Script = null; NodeIndex = -1; }
    }
}
