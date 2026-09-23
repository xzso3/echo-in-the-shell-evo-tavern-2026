using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Binding
{
    // A real region, interaction, encounter or exit component calls Raise only
    // after its own physical/gameplay condition succeeds.
    public sealed class LevelEventEmitter : MonoBehaviour
    {
        [SerializeField] private ContentIdentity endpointId;
        [SerializeField] private LevelEndpointKind kind;
        private LevelBindingSession session;
        private RuntimeScope boundScope;

        public ContentIdentity EndpointId => endpointId;
        public LevelEndpointKind Kind => kind;

        public bool Bind(LevelBindingSession bindingSession, RuntimeScope ownerScope)
        {
            if (bindingSession == null || !bindingSession.IsActive
                || ownerScope != bindingSession.Context.Scope
                || session != null && session.IsActive
                || !bindingSession.Catalog.TryGet(endpointId, out var endpoint)
                || !endpoint.IsEvent || endpoint.Kind != kind)
                return false;
            session = bindingSession;
            boundScope = ownerScope;
            return true;
        }

        public LevelEventResult Raise(Object actor = null)
        {
            if (!isActiveAndEnabled || session == null)
                return LevelEventResult.RunInactive;
            return session.Publish(boundScope, endpointId, kind, actor);
        }

        public void Unbind() { session = null; boundScope = default; }
        private void OnDestroy() { Unbind(); }
    }
}
