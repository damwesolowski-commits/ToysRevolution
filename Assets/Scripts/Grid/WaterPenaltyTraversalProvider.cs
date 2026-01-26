using Pathfinding;
using UnityEngine;

public class WaterPenaltyTraversalProvider : ITraversalProvider
{
    private readonly bool ignoreWaterPenalty;

    public WaterPenaltyTraversalProvider(bool ignoreWaterPenalty)
    {
        this.ignoreWaterPenalty = ignoreWaterPenalty;
    }

    public bool CanTraverse(Path path, GraphNode node) => true;

    public uint GetTraversalCost(Path path, GraphNode node)
    {
        if (ignoreWaterPenalty) return 0;
        if (TileLogicManager.Instance == null) return 0;

        Vector3 world = (Vector3)node.position;
        Vector2Int cell = TileLogicManager.Instance.WorldToGrid(world);

        Debug.LogWarning($"[WATER CHECK] world={world} cell={cell} isWater={TileLogicManager.Instance.IsWaterTile(cell)} ignore={ignoreWaterPenalty}");


        if (TileLogicManager.Instance.IsWaterTile(cell))
            return 1000000;

        return 0;
    }
}
