using System;
using UnityEngine;
namespace Echo.NativeGame
{
    // A real scene trigger supplies a fact; it does not complete quests or synthesize movement.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class NativeRegion : MonoBehaviour
    {
        public enum Purpose { ServiceBypass, BossEntrance }
        public Purpose purpose;
        public event Action<NativeRegion, NativePlayer> Entered;
        void OnTriggerEnter2D(Collider2D other)
        {
            var actor = other.GetComponent<NativePlayer>();
            if (actor && actor.Alive) Entered?.Invoke(this, actor);
        }
    }
}
