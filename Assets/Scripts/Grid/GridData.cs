using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Grid/GridData")]
public class GridData : ScriptableObject
{
    [System.Serializable]
    public class CellData
    {
        public bool walkable;
        public float cost = 1f;
        public bool isWater;
        public bool isSpike;
        public bool isSlippery;
    }

    public Vector2Int size;
    public Dictionary<Vector2Int, CellData> cells = new();

    public void SetCell(Vector2Int pos, CellData data)
    {
        cells[pos] = data;
    }

    public bool TryGetCell(Vector2Int pos, out CellData data)
    {
        return cells.TryGetValue(pos, out data);
    }
}
