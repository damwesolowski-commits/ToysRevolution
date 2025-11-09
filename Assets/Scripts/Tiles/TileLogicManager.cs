using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class TileLogicManager : MonoBehaviour
{
    public static TileLogicManager Instance { get; private set; }

    [Header("Tilemap odniesienia")]
    public Tilemap groundTilemap;
    public Tilemap arrowsTilemap;
    public Tilemap spikesTilemap;
    public Tilemap deadlyTilemap;
    public Tilemap slipperyTilemap;
    [Header("Assety RuleTile dla strzałek")]
    public RuleTile arrowUpTile;
    public RuleTile arrowDownTile;
    public RuleTile arrowLeftTile;
    public RuleTile arrowRightTile;
    public RuleTile arrowCrossTile;
    [Header("Assety RuleTile dla kolców")]
    public List<RuleTile> spikesTiles = new();
    [Header("Assety RuleTile dla Deadly Tiles")]
    public List<RuleTile> deadlyTiles = new();
    [Header("Assety RuleTile dla Deadly Tiles")]
    public List<RuleTile> slipperyTiles = new();
    [Header("Dane logiki mapy")]
    public GridData gridData;

    [Header("Definicje typów kafelków")]
    public Dictionary<string, TileTypeData> tileDefinitions = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 🧠 Definiujemy zachowania typów kafelków
        tileDefinitions["Grass"] = new TileTypeData("Grass", true, false, false);
        tileDefinitions["Water"] = new TileTypeData("Water", true, true, true);
        tileDefinitions["Slippery"] = new TileTypeData("Slippery", true, false, false);
        tileDefinitions["ObstacleHard"] = new TileTypeData("ObstacleHard", false, false, false);
        tileDefinitions["ObstacleSoft"] = new TileTypeData("ObstacleSoft", true, false, false);
        tileDefinitions["Arrow_Up"] = new TileTypeData("Arrow_Up", true, false, false);
        tileDefinitions["Arrow_Down"] = new TileTypeData("Arrow_Down", true, false, false);
        tileDefinitions["Arrow_Left"] = new TileTypeData("Arrow_Left", true, false, false);
        tileDefinitions["Arrow_Right"] = new TileTypeData("Arrow_Right", true, false, false);
        tileDefinitions["Arrow_Cross"] = new TileTypeData("Arrow_Cross", true, false, false);

        // Jeden wspólny obiekt dla wszystkich deadly pól lądowych
        var deadlyGround = new TileTypeData("DeadlyGround", true, true, false);

        tileDefinitions["Chasm"] = deadlyGround;
        tileDefinitions["Swamp"] = deadlyGround;
        tileDefinitions["Lava"] = deadlyGround;
        tileDefinitions["InductionHob"] = deadlyGround;
        tileDefinitions["Grease"] = deadlyGround;
    }
    // 🟢 Pamiętamy, które pola są czasowo neutralizowane przez mosty
    private HashSet<Vector3Int> neutralizedTiles = new();

    // 🔹 Sprawdza, czy dane pole jest zneutralizowane
    public bool IsTileNeutralized(Vector3 worldPos)
    {
        var cell = GetCellDataAt(worldPos);
        if (cell == null) return false;

        // Pole jest neutralizowane, jeśli znajduje się na moście (Rule Tile z flagą isBridge)
        if (cell.isBridge) return true;

        // Lub jeśli zostało tymczasowo zneutralizowane (np. przez dynamiczny most prefab)
        Vector3Int tilePos = groundTilemap.WorldToCell(worldPos);
        return neutralizedTiles.Contains(tilePos);
    }

    // 🔹 Ustawia pole jako tymczasowo neutralizowane (np. przez most)
    public void SetTileNeutralized(Vector3 worldPos, bool state)
    {
        Vector3Int cell = groundTilemap.WorldToCell(worldPos);
        if (state) neutralizedTiles.Add(cell);
        else neutralizedTiles.Remove(cell);
    }

    // 🔍 Zwraca dane komórki z GridData (zamiast z Tilemap)
    public GridData.CellData GetCellDataAt(Vector3 worldPos)
    {
        if (gridData == null)
        {
            Debug.LogWarning("⚠️ Brak przypisanego GridData w TileLogicManager!");
            return null;
        }

        Vector3Int cell = groundTilemap.WorldToCell(worldPos);
        Vector2Int gridPos = (Vector2Int)cell;

        // 🔧 Jeśli GridData nie ma jeszcze wpisu — twórz nowy
        var cellData = gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData()
            {
                position = gridPos,
                walkable = true, // ✅ domyślnie przechodnie
                isObstacleHard = false,
                isObstacleSoft = false,
                isDeadly = false,
                isSlippery = false,
                isBridge = false,
                isArrow = false,
                arrowDirection = Vector2Int.zero
            };

            // od razu zapisz w GridData (żeby system zapamiętał)
            gridData.SetCell(gridPos, cellData);
        }

        // 🧭 Automatyczne ustawienie kierunku dla Arrow Tiles (czytamy z tilemapy 'Arrows')
        // Usuwamy warunek "!cellData.isArrow", by aktualizować kierunek zawsze
        if (cellData != null && arrowsTilemap != null)

        {
            Vector3Int c = arrowsTilemap.WorldToCell(worldPos);
            var tile = arrowsTilemap.GetTile(c);
            if (tile != null)
            {
                cellData.isArrow = true;
                cellData.walkable = true; // ✅ zawsze przechodnie

                // 🔹 Porównaj z przypisanymi assetami RuleTile
                if (tile == arrowUpTile)
                    cellData.arrowDirection = Vector2Int.up;
                else if (tile == arrowDownTile)
                    cellData.arrowDirection = Vector2Int.down;
                else if (tile == arrowLeftTile)
                    cellData.arrowDirection = Vector2Int.left;
                else if (tile == arrowRightTile)
                    cellData.arrowDirection = Vector2Int.right;
                else if (tile == arrowCrossTile)
                    cellData.arrowDirection = Vector2Int.zero;
            }
        }
        // 🩸 Sprawdź, czy na tym polu są kolce (Spikes)
        if (spikesTilemap != null)
        {
            Vector3Int spikesPos = spikesTilemap.WorldToCell(worldPos);
            TileBase tileSpikes = spikesTilemap.GetTile(spikesPos);

            cellData.isSpike = false;
            cellData.isDeadly = false;

            if (tileSpikes != null && spikesTiles != null)
            {
                foreach (var spike in spikesTiles)
                {
                    if (tileSpikes == spike)
                    {
                        cellData.isSpike = true;
                        cellData.isDeadly = true;
                        break;
                    }
                }
            }
        }
        // ☠️ Sprawdź, czy na tym polu znajduje się deadly tile (lava, swamp, itp.)
        if (deadlyTilemap != null && deadlyTiles != null && deadlyTiles.Count > 0)
        {
            Vector3Int deadlyPos = deadlyTilemap.WorldToCell(worldPos);
            TileBase tileDeadly = deadlyTilemap.GetTile(deadlyPos);

            // Nie nadpisuj kolców — spikes mają wyższy priorytet
            if (!cellData.isSpike)
            {
                if (tileDeadly != null)
                {
                    foreach (var deadly in deadlyTiles)
                    {
                        if (tileDeadly == deadly)
                        {
                            cellData.isDeadly = true;
                            break;
                        }
                    }
                }
            }
        }
        return cellData;
    }

    // 🩸 Sprawdza i reaguje na wejście gracza na pole
    public void HandlePlayerOnTile(GameObject player)
    {
        var cell = GetCellDataAt(player.transform.position);
        if (cell == null) return;

        // 🟢 Ogólna obsługa przycisków
        if (cell.hasButton)
        {
            cell.button.HandleUnitStepOn(player);
        }

        PlayerInventory inv = player.GetComponent<PlayerInventory>();
        Health hp = player.GetComponent<Health>();
        if (hp == null) return;

        Animator animator = player.GetComponent<Animator>();
        GridMover mover = player.GetComponent<GridMover>();

        // 🔒 Blokada rozkazów w łańcuchu strzałek
        if (mover != null)
        {
            if (cell.isArrow) mover.SetArrowChain(true);
            else mover.SetArrowChain(false);
        }

        // 🏹 Strzałki
        if (cell.isArrow && mover != null)
            HandleArrowTile(player, cell, mover);

        // 🧊 Śliskie
        if (cell.isSlippery && mover != null)
            mover.SlideForward();

        // 🩸 Spikes – 10 HP natychmiast, most NIE chroni
        if (cell.isSpike)
        {
            hp.TakeDamage(10);
            Debug.Log($"{player.name} otrzymał 10 obrażeń od kolców");
            if (animator != null) animator.SetTrigger("Hurt");
            return; // nie przechodź dalej do deadly
        }

        // ☠️ Deadly tiles – zabijają, ale mogą być zneutralizowane
        if (cell.isDeadly
            && !cell.isSpike
            && !IsTileNeutralized(player.transform.position)
            && !IsBridgeAt(player.transform.position))
        {
            bool canSurvive = inv != null && inv.HasFloatItem;
            if (!canSurvive)
            {
                hp.TakeDamage(hp.MaxHP);
                Debug.Log($"{player.name} zginął na deadly tile ({cell.position})");
                if (animator != null) animator.SetTrigger("DefaultDeath");
            }
        }
    }

    public bool IsBridgeAt(Vector3 worldPos)
    {
        Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);
        foreach (var bridge in FindObjectsOfType<BridgeFalling>())
        {
            // 🔹 Pomijaj mosty, które już się zawaliły
            var field = bridge.GetType().GetField("isCollapsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool collapsed = (bool)field.GetValue(bridge);
            if (collapsed) continue;

            Vector3Int bridgeCell = groundTilemap.WorldToCell(bridge.transform.position);
            if (bridgeCell == cellPos)
            {
                return true;
            }
        }
        return false;
    }

    // ======================================================
    // 🌀 OBSŁUGA POLA STRZAŁEK (Arrow Tiles) – poprawiona wersja
    // ======================================================

    private System.Collections.IEnumerator ArrowMoveDelayed(GridMover mover, Vector2Int dir, GameObject player)
    {
        yield return new WaitForFixedUpdate(); // odczekaj jedną klatkę fizyki
        mover.ForceMove(dir);
        Debug.Log($"{player.name} → ArrowTile ruch w kierunku {dir} (po 1 klatce)");
    }
    private bool HandleArrowTile(GameObject player, GridData.CellData cell, GridMover mover)
    {
        if (mover.IsArrowOnCooldown())
        {
            var underTile = GetCellDataAt(player.transform.position);
            if (underTile == null || !underTile.isArrow)
                return false; // klasyczny cooldown poza strzałką
        }

        if (cell == null || !cell.isArrow) return false;

        // Kierunek z RuleTile (Up/Down/Left/Right) albo kontynuacja (Cross)
        Vector2Int dir = cell.arrowDirection != Vector2Int.zero
            ? cell.arrowDirection
            : mover.GetLastDirection();

        if (dir == Vector2Int.zero) return false;

        // Sprawdź następne pole
        Vector2Int currentTile = mover.GetCurrentTile();
        Vector2Int nextTile = currentTile + dir;
        Vector3 nextWorld = new Vector3(nextTile.x + 0.5f, nextTile.y + 0.5f, 0);
        var targetData = GetCellDataAt(nextWorld);
        if (targetData == null) return false;

        // Jeśli stoi inna jednostka — zabij ją
        Collider2D hit = Physics2D.OverlapPoint(nextWorld);
        if (hit != null && hit.gameObject != player)
        {
            Health hp = hit.gameObject.GetComponent<Health>();
            if (hp != null) hp.TakeDamage(hp.MaxHP);
        }

        // Jeżeli przechodnie — ruszamy
        if (targetData.walkable && !targetData.isObstacleHard)
        {
            if (cell.isSlippery)
            {
                // ✅ Slippery: natychmiast, żeby ślizg złapał właściwy kierunek
                mover.ForceMove(dir);
                mover.SlideForward();
            }
            else
            {
                // ✅ Ground: jak dawniej — po 1 klatce FixedUpdate (stabilnie)
                StartCoroutine(ArrowMoveDelayed(mover, dir, player));
            }
            return true;
        }

        return false;
    }
}