using System;
using UnityEngine;

namespace Echo.LevelToolkit.Foundation
{
    public enum ChunkSide { North, East, South, West }

    [Serializable]
    public struct EdgePort
    {
        [SerializeField] private bool enabled;
        [SerializeField] private int offset;
        [SerializeField] private int width;

        public bool Enabled => enabled;
        public int Offset => offset;
        public int Width => width;
        public int EndExclusive => offset + width;

        public EdgePort(bool enabled, int offset, int width)
        {
            this.enabled = enabled;
            this.offset = offset;
            this.width = width;
        }

        // North/south offsets count left to right; east/west count bottom to top.
        public bool IsValidFor(int size)
        {
            if (!enabled) return offset == 0 && width == 0;
            return size >= 3 && size <= 32 && offset >= 0 && width >= 1
                && offset < size && width <= size - offset;
        }
    }

    // Attach to a Chunk prefab root. No run state belongs on this component.
    public sealed class ChunkDefinition : MonoBehaviour
    {
        [SerializeField] private ContentIdentity contentId;
        [SerializeField] private int size = 3;
        [SerializeField] private string themeId;
        [SerializeField] private EdgePort north;
        [SerializeField] private EdgePort east;
        [SerializeField] private EdgePort south;
        [SerializeField] private EdgePort west;

        public ContentIdentity ContentId => contentId;
        public int Size => size;
        public string ThemeId => themeId;

        public EdgePort Port(ChunkSide side)
        {
            switch (side)
            {
                case ChunkSide.North: return north;
                case ChunkSide.East: return east;
                case ChunkSide.South: return south;
                case ChunkSide.West: return west;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        public bool HasValidMetadata => contentId.IsComplete && size >= 3 && size <= 32
            && north.IsValidFor(size) && east.IsValidFor(size)
            && south.IsValidFor(size) && west.IsValidFor(size);
    }

    // A placement is in integer tile coordinates; world cell size is resolved by the map host.
    [Serializable]
    public struct ChunkPlacement
    {
        [SerializeField] private ChunkDefinition chunk;
        [SerializeField] private Vector2Int cellOrigin;

        public ChunkDefinition Chunk => chunk;
        public Vector2Int CellOrigin => cellOrigin;
        public ChunkPlacement(ChunkDefinition chunk, Vector2Int cellOrigin)
        {
            this.chunk = chunk;
            this.cellOrigin = cellOrigin;
        }
    }
}
