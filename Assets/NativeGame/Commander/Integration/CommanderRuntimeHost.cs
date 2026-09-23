using Echo.NativeGame.ToolkitIntegration.LevelHost;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    // One conversation and support bridge per Native run. Settings and HTTP transport
    // live for the program, while the session is discarded when this run is destroyed.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NativeRunController))]
    public sealed class CommanderRuntimeHost : MonoBehaviour
    {
        public NativeRunController level;
        public NativeCommanderSafeNode safeNode;
        public NativeLevelHost levelHost;

        public CommanderSession Session { get; private set; }
        public CommanderSupportBridge SupportBridge { get; private set; }

        CommanderSettings settings;

        void Awake()
        {
            if (!level) level = GetComponent<NativeRunController>();
            if (!safeNode && level && level.hud && level.hud.phone)
                safeNode = level.hud.phone.safeNode;
            if (!level || !level.hud || !safeNode || safeNode.level != level)
            {
                Debug.LogError("Commander: run, HUD or safe node is not connected.", this);
                enabled = false;
                return;
            }

            SupportBridge = level.GetComponent<CommanderSupportBridge>();
            if (!SupportBridge) SupportBridge = level.gameObject.AddComponent<CommanderSupportBridge>();
            SupportBridge.level = level;
            if (!levelHost) levelHost = level.GetComponent<NativeLevelHost>();

            var snapshotSource = new CommanderSnapshotSource(level, safeNode, SupportBridge, levelHost);
            settings = CommanderSettings.Instance;
            Session = new CommanderSession(snapshotSource, CommanderHttpClient.GetOrCreate(), SupportBridge);
            settings.Changed += OnSettingsChanged;
            level.hud.BindCommanderSession(Session);
        }

        void OnSettingsChanged() { Session?.ConfigurationChanged(); }

        void OnDestroy()
        {
            if (settings != null) settings.Changed -= OnSettingsChanged;
            if (level && level.hud) level.hud.BindCommanderSession(null);
            Session?.Dispose();
            Session = null;
        }
    }
}
