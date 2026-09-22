using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace Echo.NativeGame
{
    public sealed class NativeRunController : MonoBehaviour
    {
        [Header("Scene references")]
        public NativePlayer player;
        public NativeInteraction relay;
        public NativeInteraction exit;
        public GameObject gate;
        public TMP_Text healthLabel, fireLabel, objectiveLabel, promptLabel, counterLabel, resultTitle, resultBody;
        public GameObject phonePanel, resultPanel;
        public UnityEngine.UI.Button restartButton, phoneCloseButton;
        [Header("Copy")]
        [TextArea] public string initialObjective = "01 / RECONNECT\nReach the cyan terminal. Press E nearby to open the exit.";
        public bool TerminalActivated { get; private set; }
        public bool Finished { get; private set; }
        public int Kills { get; private set; }
        public float Elapsed { get; private set; }
        public bool Running => player && player.Alive && !Finished;
        NativeInteraction nearby;

        void Awake()
        {
            Time.timeScale = 1;
            if (!player || !relay || !exit || !gate || !healthLabel || !fireLabel || !objectiveLabel || !promptLabel || !counterLabel || !resultPanel || !phonePanel || !restartButton || !phoneCloseButton)
            { Debug.LogError("NativeDemo: missing Inspector reference on Run.", this); enabled = false; return; }
            player.run = this;
            relay.run = exit.run = this;
            phonePanel.SetActive(false); resultPanel.SetActive(false);
            restartButton.onClick.AddListener(Restart);
            phoneCloseButton.onClick.AddListener(ClosePhone);
            objectiveLabel.text = initialObjective;
        }
        void Update()
        {
            if (!player) return;
            if (Running) Elapsed += Time.deltaTime;
            bool typing = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() || EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>());
            player.MoveInput = Vector2.zero;
            if (Running && !typing)
            {
                player.MoveInput = Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)), 1);
                if (Input.GetKeyDown(KeyCode.Space)) player.ToggleFire();
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    phonePanel.SetActive(!phonePanel.activeSelf);
                    ClearSelection();
                }
                if (Input.GetKeyDown(KeyCode.Escape)) ClosePhone();
                nearby = FindInteraction();
                if (Input.GetKeyDown(KeyCode.E) && nearby) nearby.Use(player);
            }
            else nearby = null;
            healthLabel.text = "SHELL  " + Mathf.CeilToInt(player.Health) + " / " + player.maxHealth;
            fireLabel.text = player.AutoFire ? "AUTO FIRE  /  SPACE TO HOLD" : "HOLD FIRE  /  SPACE TO RESUME";
            fireLabel.color = player.AutoFire ? new Color(.3f, 1, .85f) : new Color(1, .78f, .35f);
            counterLabel.text = string.Format("{0:00}:{1:00}   /   HOSTILES DISABLED  {2}", (int)Elapsed / 60, (int)Elapsed % 60, Kills);
            promptLabel.text = nearby ? nearby.Prompt : Running ? "WASD  MOVE     SPACE  FIRE / HOLD     E  INTERACT     TAB  PHONE" : "";
        }
        NativeInteraction FindInteraction()
        {
            NativeInteraction best = null; float distance = float.MaxValue;
            foreach (var target in new[] { relay, exit })
            {
                if (!target || target.Used || !target.CanReach(player)) continue;
                float d = Vector2.Distance(target.transform.position, player.transform.position);
                if (d < distance) { distance = d; best = target; }
            }
            return best;
        }
        public void ActivateRelay()
        {
            if (!Running || TerminalActivated) return;
            TerminalActivated = true; gate.SetActive(false);
            objectiveLabel.text = "02 / SIGNAL RESTORED\nThe eastern gate is open. Reach the exit and press E.";
        }
        public void ReachExit()
        {
            if (!Running || !TerminalActivated) return;
            Finished = true; player.MoveInput = Vector2.zero;
            ShowResult("CONNECTION ESTABLISHED", "Terminal reconnected. Exit reached.\n\nFirst playable checkpoint complete.\nFull story, memories and Boss encounter follow in the next milestone.");
        }
        public void PlayerDied()
        { ShowResult("SHELL OFFLINE", "Your signal was lost.\n\nKeep moving or use the side paths.\nHolding fire and opening the phone do not pause enemies."); }
        void ShowResult(string title, string body)
        {
            phonePanel.SetActive(false); resultTitle.text = title; resultBody.text = body;
            resultPanel.SetActive(true); ClearSelection();
        }
        public void RecordKill() { Kills++; }
        public void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        public void ClosePhone() { phonePanel.SetActive(false); ClearSelection(); }
        static void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
    }
}
