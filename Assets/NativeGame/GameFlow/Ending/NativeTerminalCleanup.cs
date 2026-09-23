using Echo.NativeGame.Commander;
using UnityEngine;

namespace Echo.NativeGame.GameFlow.Ending
{
    // Call after changing the run phase, before showing an ending or failure surface.
    public static class NativeTerminalCleanup
    {
        public static void StopInteractions(NativeRunController run)
        {
            if (!run) return;
            if (run.player) run.player.MoveInput = Vector2.zero;
            if (run.hud && run.hud.ActiveToolkitPlayer)
                run.hud.ActiveToolkitPlayer.MoveInput = Vector2.zero;

            var runtime = run.GetComponent<CommanderRuntimeHost>();
            runtime?.Session?.Cancel();
            runtime?.SupportBridge?.Reset();
            run.rules?.Support?.Cancel();
            if (run.hud && run.hud.phone && run.hud.phone.proxy)
                run.hud.phone.proxy.Cancel();
            if (run.dialogue) run.dialogue.Close();
            if (run.hud) run.hud.ClosePhone();
            run.PhonePause.ReleaseForRunEnd();
        }
    }
}
