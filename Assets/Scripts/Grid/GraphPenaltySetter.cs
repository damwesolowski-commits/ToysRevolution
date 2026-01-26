using UnityEngine;
using Pathfinding;
using System.Collections;

public class GraphPenaltySetter : MonoBehaviour
{
    public static GraphPenaltySetter Instance { get; private set; }

    [Header("Soft Maski i ich kary (dla wszystkich jednostek)")]
    public LayerMask softMask1;
    public int softPenalty1 = 2000;

    public LayerMask softMask2;
    public int softPenalty2 = 4000;

    public LayerMask softMask3;
    public int softPenalty3 = 6000;

    // SoftMask4 – WODA (duża kara, ale nie blokada)
    [Header("SoftMask4 – WODA (duża kara, ale nie blokada)")]
    public LayerMask softMask4;
    public int softPenalty4 = 1000000;

    [Header("Tagi TileLogic")]
    [Tooltip("Tag dla pól Deadly (np. woda)")]
    [Range(0, 31)] public int deadlyTag = 1;

    [Tooltip("Tag dla kolców (Spikes)")]
    [Range(0, 31)] public int spikesTag = 2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Istnieje więcej niż jeden GraphPenaltySetter w scenie.");
        }
        Instance = this;
    }

    private IEnumerator Start()
    {
        // 👇 Poczekaj jedną klatkę aż TileLogicManager i tilemapy się zainicjalizują
        yield return null;

        // 👇 Teraz dopiero policz kary – będą poprawne już na pierwszym ruchu
        ApplyPenalties();

        Debug.Log("GraphPenaltySetter: kary policzone po starcie gry.");
    }

    /// <summary>
    /// Liczy karę za ruch w danym worldPos:
    /// - jeśli jest most (RuleTile lub BridgeFalling) → 0
    /// - w przeciwnym razie: softMask1–4
    /// </summary>
    private uint CalculatePenaltyForPosition(Vector3 worldPos)
    {
        // 0️⃣ Most (RuleTile albo BridgeFalling) zawsze neutralizuje kary
        if (TileLogicManager.Instance != null &&
            TileLogicManager.Instance.IsBridgeAt(worldPos))
        {
            return 0;
        }

        uint penalty = 0;

        // 1️⃣ SoftMaski 1–3 (tak jak było)
        if (Physics2D.OverlapPoint(worldPos, softMask1))
            penalty = (uint)softPenalty1;
        else if (Physics2D.OverlapPoint(worldPos, softMask2))
            penalty = (uint)softPenalty2;
        else if (Physics2D.OverlapPoint(worldPos, softMask3))
            penalty = (uint)softPenalty3;

        // 2️⃣ Woda – LICZONA IDENTYCZNIE JAK W GridMoverze
        var tlm = TileLogicManager.Instance;
        if (tlm != null)
        {
            Vector3Int tilePos = tlm.groundTilemap.WorldToCell(worldPos);
            Vector2Int gridPos = (Vector2Int)tilePos;

            if (tlm.IsWaterTile(gridPos))
            {
                // duża kara za wodę, ale nie blokada
                penalty += (uint)softPenalty4;
            }
        }

        return penalty;
    }

    private void ApplyPenalties()
    {
        var gridGraph = AstarPath.active.data.gridGraph;
        if (gridGraph == null)
        {
            Debug.LogError("GraphPenaltySetter: Nie znaleziono GridGraph!");
            return;
        }

        if (TileLogicManager.Instance == null)
        {
            Debug.LogError("GraphPenaltySetter: Brak TileLogicManager.Instance!");
            return;
        }

        int deadlyCount = 0;
        int spikesCount = 0;

        foreach (var node in gridGraph.nodes)
        {
            if (node == null) continue;

            Vector3 worldPos = (Vector3)node.position;

            uint penalty = CalculatePenaltyForPosition(worldPos);
            node.Penalty = penalty;

            // 2) Tag z TileLogic (Deadly / Spikes) – zostaje bez zmian
            var cell = TileLogicManager.Instance.GetCellDataAt(worldPos);

            if (cell != null)
            {
                if (cell.isSpike)
                {
                    node.Tag = (uint)spikesTag;
                    spikesCount++;
                }
                else if (cell.isDeadly)
                {
                    node.Tag = (uint)deadlyTag;
                    deadlyCount++;
                }
                else
                {
                    if (node.Tag == deadlyTag || node.Tag == spikesTag)
                        node.Tag = 0;
                }
            }
        }

        Debug.Log($"✅ Tagi z TileLogic nadane. Deadly nodes = {deadlyCount}, Spikes nodes = {spikesCount}");
    }


    /// <summary>
    /// Stare pełne odświeżenie – jakby co wciąż działa.
    /// </summary>
    public void RefreshPenalties()
    {
        ApplyPenalties();
    }

    /// <summary>
    /// NOWE: przelicz TYLKO jeden node w okolicy worldPos.
    /// Użyjemy tego przy zawalaniu mostu.
    /// </summary>
    public void RefreshPenaltyAtPosition(Vector3 worldPos)
    {
        var gridGraph = AstarPath.active.data.gridGraph;
        if (gridGraph == null)
            return;

        var nodeInfo = gridGraph.GetNearest(worldPos);
        var node = nodeInfo.node;
        if (node == null)
            return;

        // 🔹 Kara za ruch (uwzględnia mosty i wodę)
        uint penalty = CalculatePenaltyForPosition((Vector3)node.position);
        node.Penalty = penalty;

        // 🔹 Tag z TileLogic (Deadly / Spikes)
        if (TileLogicManager.Instance != null)
        {
            var cell = TileLogicManager.Instance.GetCellDataAt(worldPos);
            if (cell != null)
            {
                if (cell.isSpike)
                    node.Tag = (uint)spikesTag;
                else if (cell.isDeadly)
                    node.Tag = (uint)deadlyTag;
                else if (node.Tag == deadlyTag || node.Tag == spikesTag)
                    node.Tag = 0;
            }
        }
    }
}
