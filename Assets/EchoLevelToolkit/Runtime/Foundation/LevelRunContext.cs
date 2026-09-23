using System;
using UnityEngine;

namespace Echo.LevelToolkit.Foundation
{
    // Implemented by the independent preview host or the Native host adapter.
    // Consumers receive it explicitly when they are initialized.
    public interface ILevelRunContext
    {
        RunId RunId { get; }
        bool IsRunning { get; }
        Transform Player { get; }
        void ReportPlayerDeath();
    }

    public sealed class LevelInstanceContext
    {
        public ILevelRunContext Run { get; }
        public RuntimeScope Scope { get; }
        public bool IsRunning => Run.IsRunning;
        public Transform Player => Run.Player;

        public LevelInstanceContext(ILevelRunContext run, RuntimeScope scope)
        {
            Run = run ?? throw new ArgumentNullException(nameof(run));
            if (!scope.RunId.IsValid || !scope.InstanceId.IsValid || scope.RunId != run.RunId)
                throw new ArgumentException("Scope must belong to the supplied run context.", nameof(scope));
            Scope = scope;
        }
    }
}
