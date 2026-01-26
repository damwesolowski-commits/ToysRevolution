using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class ColorBlock : MonoBehaviour
{

    [Header("Group Settings")]
    public int groupId; // ⬅️ ID grupy, do której klocek należy
    public enum BlockType
    {
        Deadly,    // zabija gracza przy kontakcie
        Bridge,    // most, itp.
    }

    public BlockType blockType = BlockType.Deadly;

    [Header("Ustawienia siatki")]
    public bool isExtendedAtStart = true; // startowy stan
    private bool isExtended;
    public bool IsExtended => isExtended;

    public bool IsDeadlyNow => blockType == BlockType.Deadly && isExtended;

    [Header("Sprites")]
    public Sprite extendedSprite; // wysunięty
    public Sprite hiddenSprite;   // schowany

    [Header("Dźwięki")]
    public AudioClip extendedSound; // dźwięk wysunięcia
    public AudioClip hiddenSound;   // dźwięk schowania

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector2Int gridPos;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        SetState(isExtendedAtStart, initialize: true);
    }

    // 🔹 Ustawia stan bloku (true = wysunięty, false = schowany)
    public void SetState(bool extended, bool initialize = false)
    {
        bool stateChanged = (extended != isExtended);
        isExtended = extended;
        spriteRenderer.sprite = isExtended ? extendedSprite : hiddenSprite;

        // 🧱 aktualizacja w GridData i A*
        UpdateGridState();
        UpdateAstar();

        // 🔹 Jeśli klocek się wysunął i jest deadly — sprawdź wszystkich graczy
        if (isExtended && blockType == BlockType.Deadly)
        {
            TileLogicManager tileLogic = TileLogicManager.Instance;
            if (tileLogic != null)
            {
                Collider2D[] allUnits = Physics2D.OverlapCircleAll(transform.position, 0.3f);
                foreach (var unit in allUnits)
                {
                    GridMover mover = unit.GetComponent<GridMover>();
                    if (mover != null)
                    {
                        var health = mover.GetComponent<Health>();
                        if (health != null)
                        {
                            // 🔸 Sprawdź, czy ten gracz stoi dokładnie nad deadly kaflem
                            var cell = tileLogic.gridData.GetCell((Vector2Int)tileLogic.groundTilemap.WorldToCell(transform.position));
                            if (cell != null && cell.isDeadly)
                            {
                                health.TakeDamage(9999);
                                //Debug.Log($"[ColorBlock] {mover.name} zginął, bo deadly klocek wysunął się pod nim.");
                            }
                        }
                    }
                }
            }
        }

        // 🔊 Odtwórz dźwięk i wyświetl log (tylko jeśli nie jest to inicjalizacja i stan się zmienił)
        if (!initialize && stateChanged)
        {
            if (isExtended && extendedSound != null)
                audioSource.PlayOneShot(extendedSound);
            else if (!isExtended && hiddenSound != null)
                audioSource.PlayOneShot(hiddenSound);
        }
    }
    private void UpdateGridState()
    {
        if (TileLogicManager.Instance == null || TileLogicManager.Instance.gridData == null)
            return;

        Vector3Int cell = TileLogicManager.Instance.groundTilemap.WorldToCell(transform.position);
        gridPos = new Vector2Int(cell.x, cell.y);

        var cellData = TileLogicManager.Instance.gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData();
            TileLogicManager.Instance.gridData.SetCell(gridPos, cellData);
        }

        // --- LOGIKA ZALEŻNA OD TYPU BLOKU ---
        switch (blockType)
        {
            case BlockType.Bridge:
                // Most: zawsze przechodni, ale tylko gdy wysunięty — neutralizuje deadly tiles
                cellData.walkable = true;
                cellData.isObstacleHard = false;
                cellData.isDeadly = false;
                cellData.isBridge = isExtended;

                // 🔹 Neutralizuj deadly tiles tylko gdy most wysunięty
                if (TileLogicManager.Instance != null)
                    TileLogicManager.Instance.SetTileNeutralized(transform.position, isExtended);
                break;

            case BlockType.Deadly:
                cellData.walkable = !isExtended;
                cellData.isObstacleHard = isExtended;
                cellData.isDeadly = isExtended;
                cellData.isBridge = false;

                // ❌ deadly blok nigdy nie neutralizuje
                if (TileLogicManager.Instance != null)
                    TileLogicManager.Instance.SetTileNeutralized(transform.position, false);
                break;
        }

        TileLogicManager.Instance.gridData.SetCell(gridPos, cellData);
    }

    private void UpdateAstar()
    {
        if (AstarPath.active == null || TileLogicManager.Instance == null) return;

        // Zgodnie z tym, co zapisaliśmy w gridData:
        bool walkableForAstar = true;
        switch (blockType)
        {
            case BlockType.Bridge:
                walkableForAstar = isExtended;
                break;
            case BlockType.Deadly:
                walkableForAstar = !isExtended;
                break;
        }

        Vector3 worldCenter = TileLogicManager.Instance.groundTilemap.CellToWorld(
            new Vector3Int(gridPos.x, gridPos.y, 0)
        ) + new Vector3(0.5f, 0.5f, 0);

        var bounds = new Bounds(worldCenter, Vector3.one);
        var guo = new Pathfinding.GraphUpdateObject(bounds)
        {
            updatePhysics = false,
            modifyWalkability = true,
            setWalkability = walkableForAstar
        };

        AstarPath.active.UpdateGraphs(guo);
        AstarPath.active.FlushGraphUpdates();
    }

    public void Toggle()
    {
        SetState(!isExtended);
    }

    public void ReapplyForAstar()
    {
        SetState(isExtended, initialize: false);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Jeśli spriteRenderer istnieje — aktualizuj widoczność w edytorze
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Aktualizuj sprite na podstawie flagi "Is Extended At Start"
        if (spriteRenderer != null)
            spriteRenderer.sprite = isExtendedAtStart ? extendedSprite : hiddenSprite;
    }
#endif
}
