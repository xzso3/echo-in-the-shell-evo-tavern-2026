using UnityEngine;

namespace Echo.LevelToolkit.Map
{
    // LocalId is stable within one Chunk, including when a decoration is moved.
    public sealed class ChunkDecoration : MonoBehaviour
    {
        [SerializeField] private string localId;
        public string LocalId => localId;
    }
}
