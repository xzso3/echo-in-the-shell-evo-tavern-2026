using System.Collections.Generic;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Map
{
    public static class ChunkMapGeometry
    {
        public static ValidationReport Inspect(IReadOnlyList<ChunkPlacement> placements)
        {
            var report = new ValidationReport();
            var occupied = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < placements.Count; i++)
            {
                var item = placements[i];
                if (item.Chunk == null || !item.Chunk.HasValidMetadata)
                {
                    report.Add(new ValidationIssue(IssueSeverity.Error, "MAP_INVALID_CHUNK",
                        "Chunk reference or metadata is invalid.", "Set a complete identity, size 3–32 and valid ports.", item.Chunk));
                    continue;
                }
                int n = item.Chunk.Size;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var cell = item.CellOrigin + new Vector2Int(x, y);
                    if (occupied.TryGetValue(cell, out int other))
                        report.Add(new ValidationIssue(IssueSeverity.Error, "MAP_OVERLAP",
                            $"Chunk {i} overlaps chunk {other} at cell {cell}.",
                            "Move a Chunk to an unoccupied integer origin.", item.Chunk, cell));
                    else
                        occupied.Add(cell, i);
                }
            }

            for (int i = 0; i < placements.Count; i++)
            {
                var item = placements[i];
                if (item.Chunk == null || !item.Chunk.HasValidMetadata) continue;
                foreach (ChunkSide side in System.Enum.GetValues(typeof(ChunkSide)))
                {
                    var port = item.Chunk.Port(side);
                    if (!port.Enabled) continue;
                    for (int offset = port.Offset; offset < port.EndExclusive; offset++)
                    {
                        var inside = BoundaryCell(item, side, offset);
                        var outside = inside + Normal(side);
                        if (!occupied.TryGetValue(outside, out int otherIndex) || otherIndex == i)
                        {
                            report.Add(new ValidationIssue(IssueSeverity.Warning, "MAP_OPEN_EDGE",
                                "Open port cell has no adjacent Chunk.",
                                "Place a matching Chunk or explicitly seal this edge at the level layer.",
                                item.Chunk, inside, side));
                            continue;
                        }
                        var other = placements[otherIndex];
                        var facing = Opposite(side);
                        var otherPort = other.Chunk.Port(facing);
                        int otherOffset = (side == ChunkSide.East || side == ChunkSide.West)
                            ? outside.y - other.CellOrigin.y : outside.x - other.CellOrigin.x;
                        if (!otherPort.Enabled || otherOffset < otherPort.Offset || otherOffset >= otherPort.EndExclusive)
                            report.Add(new ValidationIssue(IssueSeverity.Error, "MAP_PORT_MISMATCH",
                                "Adjacent Chunk does not open the facing cell.",
                                "Align both port intervals or seal the unmatched cell.",
                                item.Chunk, inside, side));
                    }
                }
            }
            return report;
        }

        private static Vector2Int BoundaryCell(ChunkPlacement item, ChunkSide side, int offset)
        {
            int n = item.Chunk.Size;
            switch (side)
            {
                case ChunkSide.North: return item.CellOrigin + new Vector2Int(offset, n - 1);
                case ChunkSide.East: return item.CellOrigin + new Vector2Int(n - 1, offset);
                case ChunkSide.South: return item.CellOrigin + new Vector2Int(offset, 0);
                default: return item.CellOrigin + new Vector2Int(0, offset);
            }
        }

        public static Vector2Int Normal(ChunkSide side)
        {
            switch (side)
            {
                case ChunkSide.North: return Vector2Int.up;
                case ChunkSide.East: return Vector2Int.right;
                case ChunkSide.South: return Vector2Int.down;
                default: return Vector2Int.left;
            }
        }

        public static ChunkSide Opposite(ChunkSide side) => (ChunkSide)(((int)side + 2) % 4);
    }
}
