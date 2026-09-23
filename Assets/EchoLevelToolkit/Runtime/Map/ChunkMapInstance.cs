using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Map
{
    [RequireComponent(typeof(ChunkDefinition))]
    public sealed class ChunkMapInstance : MonoBehaviour
    {
        [SerializeField] private Vector2Int cellOrigin;
        public Vector2Int CellOrigin => cellOrigin;

        public void SetCellOrigin(Vector2Int value) => cellOrigin = value;
    }
}
