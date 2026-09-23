using UnityEngine;

namespace Echo.NativeGame
{
    // One authored room tile. The same prefab can be placed repeatedly by matching its ports.
    public sealed class NativeChunk : MonoBehaviour
    {
        public Transform southPort;
        public Transform northPort;
    }
}
