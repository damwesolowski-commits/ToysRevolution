using UnityEngine;
using Pathfinding;

public class GraphPenaltySetter : MonoBehaviour
{
    [Header("Soft Maski i ich kary")]
    [Tooltip("Warstwa Obstacles Soft 1")]
    public LayerMask softMask1;
    public int softPenalty1 = 2000;

    [Tooltip("Warstwa Obstacles Soft 2")]
    public LayerMask softMask2;
    public int softPenalty2 = 4000;

    [Tooltip("Warstwa Obstacles Soft 3")]
    public LayerMask softMask3;
    public int softPenalty3 = 6000;

    void Start()
    {
        ApplyPenalties();
    }

    void ApplyPenalties()
    {
        var gridGraph = AstarPath.active.data.gridGraph;
        if (gridGraph == null)
        {
            Debug.LogError("Nie znaleziono GridGraph!");
            return;
        }

        foreach (var node in gridGraph.nodes)
        {
            Vector3 worldPos = (Vector3)(Vector3)node.position;

            // Sprawdź każdą maskę po kolei
            if (Physics2D.OverlapPoint(worldPos, softMask1))
            {
                node.Penalty = (uint)softPenalty1;
            }
            else if (Physics2D.OverlapPoint(worldPos, softMask2))
            {
                node.Penalty = (uint)softPenalty2;
            }
            else if (Physics2D.OverlapPoint(worldPos, softMask3))
            {
                node.Penalty = (uint)softPenalty3;
            }
        }

        Debug.Log("✅ Kary dla trzech Soft Mask zostały nadane.");
    }
    public void RefreshPenalties()
    {
        ApplyPenalties();
    }

}
