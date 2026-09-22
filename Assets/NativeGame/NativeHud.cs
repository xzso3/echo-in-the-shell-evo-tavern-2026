using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
namespace Echo.NativeGame
{
    // Concrete input/presentation component. Reads module state; never owns quest/combat/door state.
    public sealed class NativeHud : MonoBehaviour
    {
        public NativeRunController level;
        public NativeInteraction[] interactables;
        public TMP_Text healthLabel, fireLabel, objectiveLabel, promptLabel, counterLabel, resultTitle, resultBody;
        public GameObject phonePanel, resultPanel;
        public TMP_Text phoneArchive;
        public NativePhone phone;
        public UnityEngine.UI.Button restartButton, phoneCloseButton;
        void Awake()
        {
            phonePanel.SetActive(false); resultPanel.SetActive(false);
            restartButton.onClick.AddListener(level.Restart); phoneCloseButton.onClick.AddListener(ClosePhone);
        }
        void Update()
        {
            var player = level.player; var combat = level.combat; var dialogue = level.dialogue;
            bool typing = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() || EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>());
            player.MoveInput = Vector2.zero;
            NativeInteraction nearby = null;
            if (!typing && level.Phase == NativeRunController.RunPhase.Ending && Input.GetKeyDown(KeyCode.E)) level.endingSequence.FirstPunch();
            if (!typing && level.Phase == NativeRunController.RunPhase.Completed && Input.GetKeyDown(KeyCode.Tab)) TogglePhone();
            if (level.Running && !typing)
            {
                player.MoveInput = Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)), 1);
                if (Input.GetKeyDown(KeyCode.Space)) combat.ToggleFire();
                if (Input.GetKeyDown(KeyCode.Tab)) TogglePhone();
                if (Input.GetKeyDown(KeyCode.Return) && phonePanel.activeSelf && phone) phone.ConfirmSupport();
                if (Input.GetKeyDown(KeyCode.Escape)) { ClosePhone(); dialogue.Close(); }
                nearby = NativeInteraction.FindNearest(interactables, player);
                // An E that opens a dialogue cannot also close that newly created session.
                if (Input.GetKeyDown(KeyCode.E))
                { if (dialogue.IsOpen) dialogue.Close(); else if (level.rules.CanInteractBossCore(player)) level.rules.InteractBossCore(player); else if (nearby) nearby.Use(player); }
            }
            healthLabel.text = "SHELL  " + Mathf.CeilToInt(player.Health) + " / " + player.maxHealth;
            fireLabel.text = combat.AutoFire ? "AUTO FIRE  /  SPACE TO HOLD" : "HOLD FIRE  /  SPACE TO RESUME";
            fireLabel.color = combat.AutoFire ? new Color(.3f, 1, .85f) : new Color(1, .78f, .35f);
            objectiveLabel.text = level.quest.ObjectiveText;
            if (!phone && phoneArchive && phonePanel.activeSelf && level.narrative) phoneArchive.text = level.narrative.MemorySummary();
            counterLabel.text = string.Format("{0:00}:{1:00}   /   HOSTILES DISABLED  {2}", (int)level.Elapsed / 60, (int)level.Elapsed % 60, combat.Kills);
            promptLabel.text = dialogue.IsOpen ? "E  /  ACKNOWLEDGE TRANSMISSION" : level.rules.CanInteractBossCore(player) ? "E  /  ACT ON THE EXPOSED CORE" : nearby ? nearby.Prompt : level.Running ? "WASD  MOVE     SPACE  FIRE / HOLD     E  INTERACT     TAB  PHONE" : "";
        }
        public void ShowResult(string title, string body)
        { phonePanel.SetActive(false); resultTitle.text = title; resultBody.text = body; resultPanel.SetActive(true); ClearSelection(); }
        public void TogglePhone() { phonePanel.SetActive(!phonePanel.activeSelf); ClearSelection(); }
        public void ClosePhone() { phonePanel.SetActive(false); if (phone) phone.CloseDecision(); ClearSelection(); }
        static void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
    }
}
