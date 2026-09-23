using UnityEngine;
using UnityEngine.Tilemaps;

namespace Echo.LevelToolkit.Map
{
    // TileBase deliberately includes ordinary Tiles and RuleTiles from a shared palette.
    public sealed class ChunkTileLayers : MonoBehaviour
    {
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap obstacles;
        [SerializeField] private Tilemap decoration;
        [SerializeField] private Tilemap overhead;

        public Grid Grid => grid;
        public Tilemap Ground => ground;
        public Tilemap Obstacles => obstacles;
        public Tilemap Decoration => decoration;
        public Tilemap Overhead => overhead;
        public bool IsComplete => grid != null && ground != null && obstacles != null
            && decoration != null && overhead != null;

        public Tilemap Get(ChunkTileLayer layer)
        {
            switch (layer)
            {
                case ChunkTileLayer.Ground: return ground;
                case ChunkTileLayer.Obstacles: return obstacles;
                case ChunkTileLayer.Decoration: return decoration;
                case ChunkTileLayer.Overhead: return overhead;
                default: return null;
            }
        }
    }

    public enum ChunkTileLayer { Ground, Obstacles, Decoration, Overhead }
}
