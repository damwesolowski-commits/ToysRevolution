using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BridgeFalling : MonoBehaviour
{
    [Header("Czas do zapadnięcia (sekundy)")]
    public float collapseDelay = 1.7f;

    [Header("Efekty")]
    public AudioSource crumbleSound;
    public Animator animator;
    [Header("Wygląd po zawaleniu")]
    public Sprite brokenSprite; // sprite zawalonego mostu, ustawiany w prefabie


    private bool isTriggered = false;
    private bool isCollapsed = false;
    private float timer = 0f;

    private TileLogicManager tileLogic;

    void Start()
    {
        tileLogic = TileLogicManager.Instance;

        // 🔹 Ustal pozycję mostu na siatce
        Vector3Int bridgeCell = tileLogic.groundTilemap.WorldToCell(transform.position);
        Vector2Int gridPos = (Vector2Int)bridgeCell;

        // 🔹 Pobierz lub utwórz dane komórki w GridData
        var cell = tileLogic.gridData.GetCell(gridPos);
        if (cell == null)
        {
            cell = new GridData.CellData()
            {
                position = gridPos,
                walkable = true,
                isObstacleHard = false,
                isObstacleSoft = false,
                isDeadly = false,
                isBridge = true,
                isArrow = false,
                arrowDirection = Vector2Int.zero
            };
            tileLogic.gridData.SetCell(gridPos, cell);
        }
        else
        {
            cell.walkable = true;
            cell.isBridge = true;
            cell.isObstacleHard = false;
            cell.isObstacleSoft = false;
            cell.isDeadly = false;
        }

        // 🔹 Aktualizacja A* GridGraph
        var gridGraph = AstarPath.active.data.gridGraph;
        if (gridGraph != null)
        {
            var nodeInfo = gridGraph.GetNearest(new Vector3(bridgeCell.x + 0.5f, bridgeCell.y + 0.5f, 0));
            var node = nodeInfo.node;
            if (node != null)
            {
                node.Walkable = true;
                node.Penalty = 0;
            }

            // pełne odświeżenie grafu po zmianach
            AstarPath.active.Scan();
        }

        Debug.Log($"🌉 Most dodany do GridData i zarejestrowany jako przechodni ({transform.position})");
    }

    void Update()
    {
        if (isCollapsed) return;

        // 🔍 Sprawdź, czy na moście stoi jednostka lub kula
        if (!isTriggered && IsSomethingOnBridge())
        {
            StartCollapse();
        }

        if (isTriggered)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                Collapse();
        }
    }

    private bool IsSomethingOnBridge()
    {
        Vector3Int bridgeCell = tileLogic.groundTilemap.WorldToCell(transform.position);

        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            Vector3Int playerCell = tileLogic.groundTilemap.WorldToCell(player.transform.position);
            if (playerCell == bridgeCell)
                return true;
        }

        foreach (var ball in GameObject.FindGameObjectsWithTag("Ball"))
        {
            Vector3Int ballCell = tileLogic.groundTilemap.WorldToCell(ball.transform.position);
            if (ballCell == bridgeCell)
                return true;
        }

        return false;
    }

    private void StartCollapse()
    {
        isTriggered = true;
        timer = collapseDelay;

        if (animator != null)
            animator.SetTrigger("StartCollapse");

        if (crumbleSound != null)
            crumbleSound.Play();

        Debug.Log($"⏳ Most zaczyna się zapadać ({name})");
    }

    private void Collapse()
    {
        isCollapsed = true;

        if (animator != null)
            animator.SetTrigger("Collapse");

        var sr = GetComponent<SpriteRenderer>();
        var col = GetComponent<BoxCollider2D>();
        if (sr) sr.enabled = false;
        if (col) col.enabled = false;

        // 🔹 deadly wraca
        var cell = tileLogic.GetCellDataAt(transform.position);
        if (cell != null)
        {
            cell.isDeadly = true;
            cell.isBridge = false;
        }

        // 🔹 Zniszcz jednostki, które stoją na moście
        Vector2 size = new Vector2(0.9f, 0.9f);
        var hits = Physics2D.OverlapBoxAll(transform.position, size, 0f);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player") || h.CompareTag("Ball"))
            {
                TileLogicManager.Instance.HandlePlayerOnTile(h.gameObject);
            }
        }

        // 🔹 Zamień sprite na „zawalony most” (ustawiany w prefabie)
        if (sr != null && brokenSprite != null)
        {
            sr.enabled = true;
            sr.sprite = brokenSprite;
        }
        Debug.Log($"💥 Most {name} zawalił się!");
    }

    public void TriggerCollapse()
    {
        if (!isTriggered && !isCollapsed)
            StartCollapse();
    }
}
