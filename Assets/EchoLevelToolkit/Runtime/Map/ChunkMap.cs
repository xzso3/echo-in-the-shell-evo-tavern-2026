using System.Collections.Generic;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Map
{
    // Scene owner of independent prefab instances. The size is a host setting until physical play is checked.
    public sealed class ChunkMap : MonoBehaviour
    {
        [SerializeField, Min(0.001f)] private float cellWorldSize = 1f;
        public float CellWorldSize => cellWorldSize;

        public Vector3 CellToLocal(Vector2Int cell)
        {
            return new Vector3(cell.x * cellWorldSize, cell.y * cellWorldSize, 0f);
        }

        public List<ChunkPlacement> GetPlacements()
        {
            var result = new List<ChunkPlacement>();
            foreach (var instance in GetComponentsInChildren<ChunkMapInstance>(true))
            {
                if (instance.transform == transform || instance.GetComponent<ChunkDefinition>() == null)
                    continue;
                result.Add(new ChunkPlacement(instance.GetComponent<ChunkDefinition>(), instance.CellOrigin));
            }
            return result;
        }
    }

}
