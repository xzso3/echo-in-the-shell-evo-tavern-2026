using System;
namespace Echo.NativeGame
{
    // Small scene-level contract for parallel Boss/main-line work; no service registry or simulation adapter.
    // Implement on the actual NativeBossController MonoBehaviour. Core interaction validates phase, range and Actor itself.
    public interface INativeBossEncounter
    {
        void ActivateEncounter();
        event Action Defeated;
        bool CanInteractCore(NativePlayer actor);
        bool InteractCore(NativePlayer actor);
        bool IsDefeated { get; }
    }
}
