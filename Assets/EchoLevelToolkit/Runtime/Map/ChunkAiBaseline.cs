using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Map
{
    // Stored beside the prefab, never inferred from current content on a later AI pass.
    public sealed class ChunkAiBaseline : ScriptableObject
    {
        [SerializeField] private string sourceFingerprint;
        [SerializeField] private List<AiCellSnapshot> cells = new List<AiCellSnapshot>();
        [SerializeField] private List<AiFieldSnapshot> fields = new List<AiFieldSnapshot>();
        [SerializeField] private List<AiDecorationSnapshot> decorations = new List<AiDecorationSnapshot>();

        public string SourceFingerprint => sourceFingerprint;
        public IReadOnlyList<AiCellSnapshot> Cells => cells;
        public IReadOnlyList<AiFieldSnapshot> Fields => fields;
        public IReadOnlyList<AiDecorationSnapshot> Decorations => decorations;

        public void Replace(string fingerprint, List<AiCellSnapshot> newCells,
            List<AiFieldSnapshot> newFields, List<AiDecorationSnapshot> newDecorations)
        {
            sourceFingerprint = fingerprint;
            cells = newCells;
            fields = newFields;
            decorations = newDecorations;
        }
    }

    [Serializable]
    public struct AiCellSnapshot
    {
        public ChunkTileLayer layer;
        public Vector2Int cell;
        public TileBase tile;
        public Color color;
        public Matrix4x4 transform;
        public TileFlags flags;
    }

    [Serializable]
    public struct AiFieldSnapshot
    {
        public string path;
        public string value;
    }

    [Serializable]
    public struct AiDecorationSnapshot
    {
        public string localId;
        public string fingerprint;
        public string summary;
    }

}
