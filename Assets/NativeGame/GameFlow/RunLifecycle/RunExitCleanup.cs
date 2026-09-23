using Echo.NativeGame.Commander;

namespace Echo.NativeGame.GameFlow
{
    // Called only after FlowNavigation has acquired its lock and resolved a loadable scene.
    // Settings and the shared HTTP object survive; scene-owned state does not.
    public static class RunExitCleanup
    {
        public static void PrepareToLeave(NativeRunController run)
        {
            var runtime = run ? run.GetComponent<CommanderRuntimeHost>() : null;

            // Invalidate generation before cancelling transport, including the legacy composer.
            runtime?.Session?.Cancel();
            if (run && run.hud && run.hud.phone) run.hud.phone.CancelComposition();
            CommanderHttpClient.GetOrCreate().Cancel();

            if (!run) return;
            run.rules?.Support?.Cancel();
            runtime?.Session?.Reset();
            runtime?.SupportBridge?.Reset();

            // Close local presentation, release the single pause owner, then load the scene.
            // Run-owned combat, timer and result data are discarded with this scene.
            if (run.hud) run.hud.ClosePhone();
            if (run.dialogue) run.dialogue.Close();
            run.PhonePause.ReleaseForRunEnd();
        }
    }
}
