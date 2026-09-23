using System;

namespace Echo.NativeGame.Commander
{
    public interface ICommanderSnapshotSource
    {
        bool TryCapture(Guid sessionId, out CommanderSnapshot snapshot, out string reason);
    }
}
