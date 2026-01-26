using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static GridData;

public class TileLogicManager : MonoBehaviour
{
    public static TileLogicManager Instance { get; private set; }
    public System.Action<GameObject, Vector2Int> OnUnitEnteredTile;

    [Header("Most standardowy")]
    public TileBase standardBridgeTile;

    [Header("Tilemap odniesienia")]
    public Tilemap groundTilemap;
    public Tilemap arrowsTilemap;
    public Tilemap spikesTilemap;
    public Tilemap deadlyTilemap;
    public Tilemap slipperyTilemap;
    public Tilemap obstaclesSoftTilemap;
    public Tilemap obstaclesSoftDebugTilemap;
    public Tilemap obstaclesHardTilemap;
    public Tilemap obstaclesHardDebugTilemap;
    public Tilemap bridgesTilemap;
    public Tilemap buildFieldsTilemap;

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

    [Header("Assety RuleTile dla Slippery Tiles")]
    public List<RuleTile> slipperyTiles = new();

    [Header("Deadly Water Tiles")]
    public List<TileBase> waterTiles;

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

        // twórz kopię GridData (runtime)
        gridData = Instantiate(gridData);

        // 🧠 Definiujemy zachowania typów kafelków
        tileDefinitions["Grass"] = new TileTypeData("Grass", true, false, false);
        tileDefinitions["Water"] = new TileTypeData("Water", false, true, true);
        tileDefinitions["Slippery"] = new TileTypeData("Slippery", true, false, false);
        tileDefinitions["ObstacleHard"] = new TileTypeData("ObstacleHard", false, false, false);
        tileDefinitions["ObstacleSoft"] = new TileTypeData("ObstacleSoft", true, false, false);
        tileDefinitions["Arrow_Up"] = new TileTypeData("Arrow_Up", true, false, false);
        tileDefinitions["Arrow_Down"] = new TileTypeData("Arrow_Down", true, false, false);
        tileDefinitions["Arrow_Left"] = new TileTypeData("Arrow_Left", true, false, false);
        tileDefinitions["Arrow_Right"] = new TileTypeData("Arrow_Right", true, false, false);
        tileDefinitions["Arrow_Cross"] = new TileTypeData("Arrow_Cross", true, false, false);

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
        Vector3Int cell = Vector3Int.FloorToInt(worldPos);
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

        // 1) PRZESZKODY — sprawdzamy WŁAŚCIWE tilemapy, NIE ground
        if (
            (obstaclesHardTilemap != null && obstaclesHardTilemap.GetTile(cell) != null) ||
            (obstaclesHardDebugTilemap != null && obstaclesHardDebugTilemap.GetTile(cell) != null)
           )
        {
            cellData.isObstacleHard = true;
            cellData.isObstacleSoft = false;
            cellData.walkable = false;
            cellData.isDeadly = true;
        }
        else if (
            (obstaclesSoftTilemap != null && obstaclesSoftTilemap.GetTile(cell) != null) ||
            (obstaclesSoftDebugTilemap != null && obstaclesSoftDebugTilemap.GetTile(cell) != null)
           )
        {
            // ObstaclesSoft i ObstaclesSoftDebug traktowane tak samo
            cellData.isObstacleSoft = true;
            cellData.isObstacleHard = false;
            cellData.walkable = false;
            cellData.isDeadly = true;
        }
        else
        {
            // jeśli na tym polu nie ma przeszkód, wyczyść stare flagi
            cellData.isObstacleHard = false;
            cellData.isObstacleSoft = false;
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
        // Build Fields (pola do budowy)
        if (buildFieldsTilemap != null)
        {
            Vector3Int p = buildFieldsTilemap.WorldToCell(worldPos);
            TileBase t = buildFieldsTilemap.GetTile(p);
            if (t != null)
            {
                cellData.walkable = true;
                cellData.isBuildField = true;
            }
        }
        // 💧 WODA – domyślnie NIEprzechodnia dla jednostek
        // (wyjątek z "kołem do pływania" dodamy później w innych skryptach)
        if (IsWaterTile(gridPos))
        {
            // Woda nadal jest traktowana jako deadly (ważne np. dla skrzyń)
            cellData.isDeadly = true;

            // Player / Enemy nie mogą na nią wejść
            cellData.walkable = false;
        }
        return cellData;
    }

    // 🩸 Sprawdza i reaguje na wejście gracza na pole
    public void HandlePlayerOnTile(GameObject player)
    {
        var cell = GetCellDataAt(player.transform.position);
        if (cell == null) return;
        OnUnitEnteredTile?.Invoke(player, cell.position);

        // 🧱 Czy gracz stoi na wysuniętym deadly ColorBlocku?
        if (CheckDeadlyColorBlockUnder(player))
            return;   // gracz już zginął – reszta logiki nie jest potrzebna

        // 🟢 Ogólna obsługa przycisków
        if (cell.hasButton)
        {
            cell.button.HandleUnitStepOn(player);
        }

        UnitInventory inv = player.GetComponent<UnitInventory>();
        Health hp = player.GetComponent<Health>();
        if (hp == null) return;

        Animator animator = player.GetComponent<Animator>();
        GridMover mover = player.GetComponent<GridMover>();

        // 🧱 SAFETY: jeśli z jakiegoś powodu Player stoi na Obstacles → natychmiastowa śmierć
        if (cell.isObstacleHard || cell.isObstacleSoft)
        {
            hp.TakeDamage(hp.MaxHP);
            Debug.Log($"{player.name} zginął, bo znalazł się na Obstacles ({cell.position})");
            if (animator != null) animator.SetTrigger("DefaultDeath");
            return;
        }

        // 🔒 Blokada rozkazów w łańcuchu strzałek
        if (mover != null)
        {
            if (cell.isArrow) mover.SetArrowChain(true);
            else mover.SetArrowChain(false);
        }

        // 🏹 Strzałki
        if (cell.isArrow && mover != null)
            HandleArrowTile(player, cell, mover);

        // 🧊 Śliskie (ale NIE gdy to jednocześnie Arrow – wtedy ślizg obsłuży HandleArrowTile)
        if (cell.isSlippery && mover != null && !cell.isArrow)
        {
            if (!mover.HasJustPushedBox())
                mover.SlideForward();
        }

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
            // Ustal czy stoimy NA WODZIE
            Vector3Int tilePos3 = groundTilemap.WorldToCell(player.transform.position);
            Vector2Int tilePos = (Vector2Int)tilePos3;
            bool isWater = IsWaterTile(tilePos);

            // Koło ratuje TYLKO na wodzie
            bool canSurvive = isWater && inv != null && inv.HasFloatEquipped;

            if (!canSurvive)
            {
                hp.TakeDamage(hp.MaxHP);
                Debug.Log($"{player.name} zginął na deadly tile ({cell.position})");
                if (animator != null) animator.SetTrigger("DefaultDeath");
            }
        }

        // 🌀 TELEPORT — po wejściu na teleport, natychmiast go aktywujemy
        if (cell.hasTeleport && cell.teleport != null)
        {
            cell.teleport.HandleUnitStepOn(player);
            return; // zatrzymujemy dalszą logikę, teleport przejmuje kontrolę
        }
    }
    public void HandleUnitOnTile(GameObject unit)
    {
        var cell = GetCellDataAt(unit.transform.position);
        if (cell == null) return;
        OnUnitEnteredTile?.Invoke(unit, cell.position);

        // 🧱 SPRAWDŹ, czy gracz stoi na wysuniętym deadly-klocku (ColorBlock)
        if (CheckDeadlyColorBlockUnder(unit))
            return; // gracz już zginął – dalej nie wykonujemy logiki kafelka

        // ✅ Skrzynie / wrogowie / inne jednostki też naciskają przyciski
        if (cell.hasButton && cell.button != null)
        {
            cell.button.HandleUnitStepOn(unit);
        }

        Health hp = unit.GetComponent<Health>();
        UnitInventory inv = unit.GetComponent<UnitInventory>();
        GridMover mover = unit.GetComponent<GridMover>();

        // 🔒 Blokada rozkazów w łańcuchu strzałek (Enemy / inne jednostki)
        if (mover != null)
        {
            if (cell.isArrow)
                mover.SetArrowChain(true);
            else
                mover.SetArrowChain(false);
        }

        // 1) Obstacles → natychmiastowa śmierć
        if (cell.isObstacleHard || cell.isObstacleSoft)
        {
            hp.TakeDamage(hp.MaxHP);
            return;
        }

        // 2) Spikes → obrażenia
        if (cell.isSpike)
        {
            hp.TakeDamage(10);
            return;
        }

        // 3) DeadlyTile – różne działanie dla skrzyni i jednostek
        if (cell.isDeadly && !IsTileNeutralized(unit.transform.position) && !IsBridgeAt(unit.transform.position))
        {
            // 🔵 SKRZYNIA
            if (mover != null && mover.moverType == GridMover.GridMoverType.Chest)
            {
                Vector3Int chestTilePos = groundTilemap.WorldToCell(unit.transform.position);
                Vector2Int gridPos = (Vector2Int)chestTilePos;

                // Jeśli DeadlyTile = woda → zmień w most
                if (IsWaterTile(gridPos))
                {
                    ReplaceWithStandardBridge(gridPos);
                }

                // Skrzynia znika w każdym przypadku
                Destroy(unit);
                return;
            }

            // 🔴 INNE JEDNOSTKI (gracz, wrogowie)
            Vector3Int tilePos3 = groundTilemap.WorldToCell(unit.transform.position);
            Vector2Int tilePos = (Vector2Int)tilePos3;
            bool isWater = IsWaterTile(tilePos);

            bool immune = isWater && inv != null && inv.HasFloatEquipped;
            if (!immune)
            {
                hp.TakeDamage(hp.MaxHP);
                return;
            }
        }

        // 4) Strzałki
        if (cell.isArrow && mover != null)
        {
            HandleArrowTile(unit, cell, mover);
        }

        // 5) Śliskie (ale nie Arrow+Slippery – tam ślizg odpala logika strzałek)
        if (cell.isSlippery && mover != null && !cell.isArrow)
        {
            mover.SlideForward();
        }

        // 6) Teleport
        if (cell.hasTeleport && cell.teleport != null)
        {
            cell.teleport.HandleUnitStepOn(unit);
        }
    }

    private bool CheckDeadlyColorBlockUnder(GameObject unit)
    {
        if (unit == null || groundTilemap == null)
            return false;

        // Środek kafelka, na którym stoi jednostka
        Vector3Int cell = groundTilemap.WorldToCell(unit.transform.position);
        Vector3 center = groundTilemap.GetCellCenterWorld(cell);

        // Szukamy wszystkich colliderów na tym polu
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.25f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            ColorBlock block = hit.GetComponent<ColorBlock>();
            if (block != null && block.IsDeadlyNow)
            {
                // Znaleźliśmy wysunięty deadly-klocek pod jednostką → zabijamy
                Health hp = unit.GetComponent<Health>();
                if (hp != null)
                {
                    hp.TakeDamage(hp.MaxHP);
                }

                return true;
            }
        }

        return false;
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
        // 🔹 Sprawdź, czy na Tilemapie znajduje się Rule Tile mostu
        Vector3Int cell = bridgesTilemap.WorldToCell(worldPos);
        TileBase tile = bridgesTilemap.GetTile(cell);
        if (tile != null && tile.name.ToLower().Contains("bridge"))
        {
            return true;
        }
        return false;
    }

    // ======================================================
    // 🌀 OBSŁUGA POLA STRZAŁEK (Arrow Tiles)
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

        // 🔥 Jeśli na docelowym polu stoi skrzynia → spróbuj ją popchnąć.
        // 1. Jeżeli popchnięcie jest niemożliwe → jednostka ginie na strzałce.
        // 2. Jeżeli się da → skrzynia rusza do przodu, a jednostka za chwilę wejdzie na jej stare pole.
        if (GridMover.IsCellOccupied(nextTile))
        {
            var occ = GetOccupier(nextTile);
            if (occ != null && occ.moverType == GridMover.GridMoverType.Chest)
            {
                // próbujemy popchnąć skrzynię w kierunku strzałki
                bool pushed = PushBox(occ.gameObject, dir);
                if (!pushed)
                {
                    // skrzyni nie da się przepchnąć (ściana, inna skrzynia, brak pushowalnego pola)
                    var hp = player.GetComponent<Health>();
                    if (hp != null)
                        hp.TakeDamage(hp.MaxHP);

                    return false; // ❗ strzałka przestaje działać
                }

                // ✅ Udało się popchnąć skrzynię.
                // Zaznacz jednostkę jako "właśnie pchnęła skrzynię",
                // żeby np. śliskie pole nie odpaliło od razu ślizgu po tym ruchu.
                if (mover != null)
                {
                    mover.MarkJustPushedBox();
                }
            }
        }

        var targetData = GetCellDataAt(nextWorld);
        if (targetData == null) return false;

        // 👉 Jeśli strzałka pcha jednostkę (nie skrzynię) na pole nieprzechodnie,
        // to jednostka ginie NA STRZAŁCE.
        if (mover.moverType != GridMover.GridMoverType.Chest && !targetData.walkable)
        {
            var hpSelf = player.GetComponent<Health>();
            if (hpSelf != null)
            {
                hpSelf.TakeDamage(hpSelf.MaxHP);
            }
            return false; // nie ruszamy już jednostki z miejsca
        }

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
                // ❄ Strzałka stoi na lodzie:
                // Nie ruszamy od razu – tylko planujemy ślizg w kierunku strzałki.
                // W następnym FixedUpdate GridMover wywoła SlideForward() z tym kierunkiem.
                mover.ScheduleArrowSlide(dir);
            }
            else
            {
                // Zwykła ziemia – klasyczny ruch po 1 klatce
                StartCoroutine(ArrowMoveDelayed(mover, dir, player));
            }
            return true;
        }
        return false;
    }
    public void RefreshTileLogic(Vector3Int cellPos)
    {
        if (gridData == null) return;

        Vector2Int gridPos = (Vector2Int)cellPos;
        var cellData = gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData() { position = gridPos };
            gridData.SetCell(gridPos, cellData);
        }

        // domyślnie pole jest przechodnie
        cellData.walkable = true;
        cellData.isObstacleSoft = false;
        cellData.isObstacleHard = false;
        cellData.isArrow = false;
        cellData.isDeadly = false;
        cellData.isSpike = false;

        // 1) PRZESZKODY — sprawdzamy WŁAŚCIWE tilemapy, NIE ground
        if (
            (obstaclesHardTilemap != null && obstaclesHardTilemap.GetTile(cellPos) != null) ||
            (obstaclesHardDebugTilemap != null && obstaclesHardDebugTilemap.GetTile(cellPos) != null)
           )
        {
            cellData.isObstacleHard = true;
            cellData.walkable = false;
            cellData.isDeadly = true;
        }
        else if (
            (obstaclesSoftTilemap != null && obstaclesSoftTilemap.GetTile(cellPos) != null) ||
            (obstaclesSoftDebugTilemap != null && obstaclesSoftDebugTilemap.GetTile(cellPos) != null)
           )
        {
            // ObstaclesSoft i ObstaclesSoftDebug traktowane tak samo
            cellData.isObstacleSoft = true;
            cellData.walkable = false;
            cellData.isDeadly = true;
        }

        // 2) ARROWS — przechodnie + flaga isArrow
        if (arrowsTilemap != null && arrowsTilemap.GetTile(cellPos) != null)
        {
            cellData.isArrow = true;
            cellData.walkable = true;
        }

        // 3) DEADLY / SPIKES — przechodnie, ale z obrażeniami
        if (deadlyTilemap != null && deadlyTilemap.GetTile(cellPos) != null)
        {
            cellData.isDeadly = true;
            cellData.walkable = true;
        }
        if (spikesTilemap != null && spikesTilemap.GetTile(cellPos) != null)
        {
            cellData.isSpike = true;
            cellData.isDeadly = true;
            cellData.walkable = true;
        }
        // === REAKCJA NA ZMIANĘ POLA, GDY KTOŚ JUŻ NA NIM STOI ===
        Vector3 center = new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
        var hits = Physics2D.OverlapPointAll(center);
        if (hits != null && hits.Length > 0)
        {
            foreach (var h in hits)
            {
                if (h == null) continue;

                var mover = h.GetComponent<GridMover>();
                if (mover == null) continue; // interesują nas tylko jednostki poruszające się po siatce

                var hp = mover.GetComponent<Health>();
                var inv = mover.GetComponent<UnitInventory>();

                // 1) Jeśli pojawiła się STRZAŁKA pod graczem -> natychmiast uruchom jej efekt
                if (cellData.isArrow)
                {
                    // To wywołanie używa aktualnej logiki strzałek i samo pchnie gracza w kierunku kafla (po 1 klatce Fixed)
                    HandlePlayerOnTile(mover.gameObject);
                    continue; // przejdź do następnego trafienia
                }

                // 2) Jeśli pojawił się OBSTACLE pod graczem -> natychmiastowa śmierć
                if ((cellData.isObstacleHard || cellData.isObstacleSoft) && hp != null)
                {
                    hp.TakeDamage(hp.MaxHP);
                    continue;
                }

                // 3) Jeśli pojawił się DEADLY pod graczem -> zasady jak zwykle (most/neutralizacja)
                if (cellData.isDeadly && hp != null && !cellData.isSpike)
                {
                    bool isWater = IsWaterTile((Vector2Int)cellPos);
                    bool canFloat = isWater && (inv != null && inv.HasFloatEquipped);
                    bool neutral = IsTileNeutralized(center) || IsBridgeAt(center);
                    if (!neutral && !canFloat)
                    {
                        hp.TakeDamage(hp.MaxHP);
                    }
                }

                // 4) Kolce (spikes) pojawiające się pod graczem -> od razu zadadzą obrażenia
                if (cellData.isSpike && hp != null)
                {
                    hp.TakeDamage(10);
                }

                // 4) BRIDGES — neutralizują deadly tiles
                if (groundTilemap != null)
                {
                    TileBase tile = bridgesTilemap.GetTile(cellPos);
                    if (tile != null)
                    {
                        string name = tile.name.ToLower();
                        if (name.Contains("bridge"))
                        {
                            cellData.isBridge = true;
                            cellData.walkable = true;
                            cellData.isDeadly = false;
                            cellData.isObstacleSoft = false;
                            cellData.isObstacleHard = false;
                        }
                    }
                }
            }
        }
    }
    // ======================================================
    // 🌀 SKRZYNIA (Chest)
    // ======================================================
    //Funkcja wykonująca pchnięcie (tylko jeśli TryPushBox zwróci true)
    public bool IsPushableTile(CellData cell)
    {
        if (cell == null) return false;

        if (cell.isBuildField) return true;    // Pola do budowy
        if (cell.isSlippery) return true;      // Lód
        if (cell.hasButton) return true;       // Przycisk
        if (cell.isDeadly && !cell.isSpike) return true;// DeadlyTile
        return false; // NIC INNEGO NIE JEST PUSHABLE
    }

    public bool CanChestEnter(CellData cell)
    {
        if (cell == null) return false;

        // Pola normalne
        if (cell.isBuildField) return true;
        if (cell.isSlippery) return true;
        if (cell.hasButton) return true;

        // DeadlyTile — skrzynia może wejść (ale NIE na kolce)
        if (cell.isDeadly && !cell.isSpike)
            return true;

        return false;
    }

    //Główna funkcja „czy można przepchnąć skrzynię”
    public bool TryPushBox(Vector3Int boxCell, Vector2Int direction)
    {
        // ❌ Zakaz pchania po skosie – tylko góra/dół/lewo/prawo
        if (direction.x != 0 && direction.y != 0)
            return false;

        // Pozycja za skrzynią w kierunku pchania
        Vector3Int targetCell = new Vector3Int(
            boxCell.x + direction.x,
            boxCell.y + direction.y,
            0
        );

        // pobierz dane kafelka docelowego przez TileLogicManager,
        // żeby widzieć BuildFields, Slippery itd.
        Vector3 targetWorld = GridMap.Instance.CellToWorld(targetCell);
        var targetData = GetCellDataAt(targetWorld);
        if (targetData == null) return false;

        // Jeśli pole do przesuwania → OK
        if (IsPushableTile(targetData)
            && !GridMover.IsCellOccupied(new Vector2Int(targetCell.x, targetCell.y)))
        {
            return true;
        }

        return false;
    }
    //Funkcja wykonująca pchnięcie (tylko jeśli TryPushBox zwróci true)
    public bool PushBox(GameObject box, Vector2Int direction)
    {
        if (box == null) return false;

        // Pobieramy aktualną pozycję skrzyni
        Vector3Int boxCell = GridMap.Instance.WorldToCell(box.transform.position);

        // Sprawdzamy czy można przepchnąć
        if (!TryPushBox(boxCell, direction))
            return false;

        // Wywołujemy ruch skrzyni
        var mover = box.GetComponent<GridMover>();
        if (mover != null)
            mover.ForceMove(direction);  // za chwilę dodamy to do GridMover

        return true;
    }
    private GridMover GetOccupier(Vector2Int tile)
    {
        var field = typeof(GridMover).GetField("OccupiedBy",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static);

        var dict = field.GetValue(null) as System.Collections.IDictionary;

        if (dict.Contains(tile))
            return dict[tile] as GridMover;

        return null;
    }
    // Sprawdza czy kafel jest WODĄ, ale NIE jest przykryty MOSTEM
    public bool IsWaterTile(Vector2Int cell)
    {
        Vector3Int v3 = (Vector3Int)cell;

        // 1) Jeśli na tym polu jest most → NIE traktujemy jako wodę
        if (bridgesTilemap != null)
        {
            TileBase bridgeTile = bridgesTilemap.GetTile(v3);
            if (bridgeTile != null && bridgeTile.name.ToLower().Contains("bridge"))
            {
                return false; // most neutralizuje wodę także dla pathfindingu
            }
        }

        // 2) Jeśli pole jest zneutralizowane (np. dynamiczny most prefab) → też nie jest wodą
        if (neutralizedTiles.Contains(v3))
            return false;

        // 3) Klasyczne sprawdzenie: czy na deadlyTilemap leży woda
        TileBase t = null;

        // 3) Najpierw spróbuj deadlyTilemap (jak było)
        if (deadlyTilemap != null)
            t = deadlyTilemap.GetTile(v3);

        // 4) Jeśli tam nic nie ma, sprawdź groundTilemap (to jest najczęstszy przypadek "woda namalowana na ground")
        if (t == null && groundTilemap != null)
            t = groundTilemap.GetTile(v3);

        if (t == null) return false;

        // A) jeśli masz listę waterTiles – użyj jej
        if (waterTiles != null && waterTiles.Contains(t))
            return true;

        // B) fallback: jeśli tile ma w nazwie "water"
        return t.name != null && t.name.ToLower().Contains("water");
    }


    // Zamienia wodę w most na bridgesTilemap
    public void ReplaceWithStandardBridge(Vector2Int cell)
    {
        if (bridgesTilemap == null)
        {
            Debug.LogError("Brak przypisanej bridgesTilemap!");
            return;
        }

        if (standardBridgeTile == null)
        {
            Debug.LogError("Brak ustawionego standardBridgeTile w Inspectorze!");
            return;
        }

        bridgesTilemap.SetTile((Vector3Int)cell, standardBridgeTile);

        RefreshTileLogic((Vector3Int)cell);

        Debug.Log($"Zamieniono wodę na most na pozycji {cell}");
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // używamy tej samej tilemapy, którą i tak masz w TileLogicManager
        Vector3Int cell = groundTilemap.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }
}