using UnityEngine;

namespace Echo.LevelToolkit.Map
{
    public sealed class ChunkAiSource : MonoBehaviour
    {
        [SerializeField] private ChunkAiBaseline baseline;
        public ChunkAiBaseline Baseline => baseline;
    }
}
