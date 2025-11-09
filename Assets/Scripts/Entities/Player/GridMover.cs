using Pathfinding;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridMover : MonoBehaviour
{
    [Header("Ruch")]
    public float tilesPerSecond = 1.666f;
    [Tooltip("Pola śliskie")]
    public float slipperyTilesPerSecond = 2.5f;
    public float arriveEps = 0.12f;

    [Header("Planowanie / Blokady")]
    public float blockRepathDelay = 0.35f;
    public int maxRepathAttempts = 5;
    public bool logDebug = false;

    public bool IsMoving() => moving;

    private Seeker seeker;
    private SelectableHighlight highlight;

    private List<Vector2Int> pathTiles = new();
    private int pathIndex = 0;
    private bool moving = false;

    private bool isOnArrowChain = false;

    private Vector2Int currentTile;
    private Vector2Int? nextTile;
    private Vector2Int lastDirection = Vector2Int.zero;
    // 🔒 Blokada przyjmowania nowych rozkazów podczas ślizgania
    private bool isSliding = false;
    public void SetSliding(bool value) => isSliding = value;

    private float arrowMoveCooldown = 0f;
    private float blockTimer = 0f;
    private int repathAttempts = 0;

    private Vector3 lastGoalWorld; // ostatni zapamiętany cel (do ponownego przeliczenia trasy)

    private static readonly Dictionary<Vector2Int, GridMover> OccupiedBy = new();
    private static readonly Dictionary<Vector2Int, GridMover> ReservedBy = new();
    private static readonly List<Vector2Int> s_tempTiles = new(16);

    void Awake()
    {
        seeker = GetComponent<Seeker>();
        highlight = GetComponent<SelectableHighlight>();

        Vector2 rounded = SnapCenter(transform.position);
        transform.position = rounded;
        currentTile = WorldToTile(rounded);
        Occupy(currentTile, this);
    }

    void OnDisable()
    {
        ClearAllReservationsOwnedBy(this);
        ReleaseIfOccupiedBy(currentTile, this);
    }

    void OnDestroy()
    {
        ClearAllReservationsOwnedBy(this);
        ReleaseIfOccupiedBy(currentTile, this);
    }

    void Update()
    {
        if (highlight != null && !highlight.IsSelected) return;
        // 🧊 Jeśli gracz się ślizga – nie przyjmuj nowych rozkazów
        if (isSliding) return;
        // 🏹 Jeśli gracz porusza się po strzałkach – zablokuj nowe rozkazy
        if (isOnArrowChain) return;
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            RequestPathTo(world);
        }
    }

    void FixedUpdate()
    {
        if (arrowMoveCooldown > 0f)
            arrowMoveCooldown -= Time.fixedDeltaTime;

        if (!moving) return;

        // 1) Rezerwacja następnego kafla
        if (nextTile == null && pathIndex < pathTiles.Count)
        {
            Vector2Int candidate = pathTiles[pathIndex];

            if (CanReserve(candidate, this))
            {
                Vector2 candidateCenter = TileToWorld(candidate);

                // ❗ Skos zablokowany tylko, gdy narożniki Hard.
                //    Dla dwóch Softów po skosie – zezwól (to nasz przypadek).
                if (IsDiagonalMoveBlocked(currentTile, candidate))
                {
                    if (BothSidesSoft(currentTile, candidate))
                    {
                        // pozwól przejść — nie przerywamy, nie robimy repathu
                    }
                    else
                    {
                        if (logDebug) Debug.Log($"{name}: Diagonal blocked between {currentTile} and {candidate}");
                        ClearAllReservationsOwnedBy(this);
                        TryRepath("DiagonalBlock");
                        return;
                    }
                }


                // Pobieramy dane o kafelku z logiki siatkowej
                // 🧭 Pobieramy dane o kafelku
                var candidateData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(candidate));

                // 🔹 Jeśli nic nie zwrócono – traktujemy jako pole przechodnie
                if (candidateData == null)
                {
                    if (logDebug) Debug.Log($"{name}: candidate {candidate} has no data → treating as walkable");
                }
                else
                {
                    // Hard obstacles = nieprzechodnie
                    if (candidateData.isObstacleHard)
                    {
                        ClearAllReservationsOwnedBy(this);
                        TryRepath("HARD");
                        return;
                    }

                    // Soft obstacles = omijaj
                    if (candidateData.isObstacleSoft)
                    {
                        ClearAllReservationsOwnedBy(this);
                        TryRepath("SOFT");
                        return;
                    }

                    // 💡 Strzałki są zawsze przechodnie
                    if (!candidateData.walkable && !candidateData.isArrow)
                    {
                        ClearAllReservationsOwnedBy(this);
                        TryRepath("NOT WALKABLE");
                        return;
                    }
                }

                // 🔸 Jeżeli wszystko OK — rezerwuj
                Reserve(candidate, this);
                nextTile = candidate;
                if (logDebug) Debug.Log($"{name}: Reserve {candidate}");
            }
            else
            {
                // ⬇️ JESTEŚMY NA STRZAŁCE? – pozwól ruszyć bez rezerwacji,
                // zabijemy i zajmiemy kafel przy dojeździe (patrz punkt 2B)
                var currentData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));
                if (currentData != null && currentData.isArrow)
                {
                    nextTile = candidate;              // jedziemy bez rezerwacji
                    if (logDebug) Debug.Log($"{name}: Leaving ARROW -> moving to {candidate} without reserve.");
                    // nie rób replanów – wracamy, by przejść do sekcji ruchu
                    return;
                }

                // 🔁 standardowe zachowanie (blokada + próby replanów)
                blockTimer += Time.fixedDeltaTime;
                if (blockTimer >= blockRepathDelay)
                {
                    blockTimer = 0f;
                    repathAttempts++;

                    if (repathAttempts > maxRepathAttempts)
                    {
                        moving = false;
                        ClearAllReservationsOwnedBy(this);
                        Debug.LogWarning($"{name}: Stopped after too many blocked attempts.");
                        return;
                    }

                    // przelicz ścieżkę od bieżącej pozycji do zapamiętanego celu, NIE resetując licznika
                    if (pathTiles.Count > 0)
                    {
                        Vector2Int goal = pathTiles[^1];
                        lastGoalWorld = TileToWorld(goal); // podtrzymujemy ostatni cel
                    }

                    if (logDebug) Debug.Log($"{name}: Blocked → Repath ({repathAttempts}/{maxRepathAttempts})");
                    seeker.StartPath(transform.position, lastGoalWorld, OnPathComplete);
                }
            }
        }

        // 2) Ruch do nextTile
        if (nextTile != null)
        {
            Vector2 nextCenter = TileToWorld(nextTile.Value);

            // 🧊 Jeśli jesteśmy na polu śliskim – użyj innej prędkości
            float currentSpeed = tilesPerSecond;

            // Ustal pozycję kafelka, na którym aktualnie stoi gracz
            Vector3 worldPos = TileToWorld(currentTile);
            var cellData = TileLogicManager.Instance.GetCellDataAt(worldPos);

            // Jeśli kafelek jest śliski – zwiększ prędkość
            if (cellData != null && cellData.isSlippery)
                currentSpeed = slipperyTilesPerSecond;

            float step = currentSpeed * Time.fixedDeltaTime;
            Vector2 newPos = Vector2.MoveTowards(transform.position, nextCenter, step);
            transform.position = newPos;

            Debug.DrawLine(transform.position, nextCenter, Color.green, 0.02f);


            float dist = Vector2.Distance(newPos, nextCenter);
            float fullDist = Vector2.Distance(SnapCenter(TileToWorld(currentTile)), nextCenter);

            // (A) Zwolnij obecny kafel trochę wcześniej – ok. 55% drogi
            float progressEarlyRelease = 0.55f;
            if (fullDist > 0.0001f && (1f - (dist / fullDist)) >= progressEarlyRelease)
            {
                ReleaseIfOccupiedBy(currentTile, this);
            }

            // (B) Gdy jesteśmy blisko środka następnego kafla
            if (dist <= step) // zamiast arriveEps
            {
                // płynne „dosunięcie” — bez skoku
                transform.position = nextCenter;

                // 🔸 Jeśli na poprzednim kafelku był przycisk — powiadom go o zejściu
                var previousCell = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));
                if (previousCell != null && previousCell.hasButton)
                {
                    previousCell.button.HandleUnitStepOff(gameObject);
                }

                if (TryOccupy(nextTile.Value, this))
                {
                    // 🟦 ZAPAMIĘTAJ KIERUNEK zanim podmienisz currentTile
                    Vector2Int enteredDir = nextTile.Value - currentTile;

                    ClearReservationIfOwnedBy(nextTile.Value, this);
                    currentTile = nextTile.Value;
                    lastDirection = enteredDir;

                    // 🔹 Sprawdź logikę pola (np. kolce, śliskie, strzałki)
                    if (TileLogicManager.Instance != null)
                    {
                        TileLogicManager.Instance.HandlePlayerOnTile(gameObject);
                    }
                    // 🕐 Ustaw cooldown tylko jeśli aktualne pole to strzałka
                    var arrivedCell = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));
                    if (arrivedCell != null && arrivedCell.isArrow)
                        arrowMoveCooldown = 0.05f;
                    else
                        arrowMoveCooldown = 0f;

                    nextTile = null;
                    pathIndex++;
                    blockTimer = 0f;
                    repathAttempts = 0;

                    if (pathIndex >= pathTiles.Count)
                    {
                        moving = false;
                        if (logDebug) Debug.Log($"{name}: Arrived {currentTile}");

                        // 🔓 Odblokuj sterowanie dopiero po zejściu z lodu
                        var arrivedCell2 = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));
                        bool stillOnIce = arrivedCell2 != null && arrivedCell2.isSlippery;
                        if (!stillOnIce)
                            isSliding = false;
                    }

                    // 🔄 Zaktualizuj stan zajętości po zakończeniu ruchu
                    AstarPath.active.AddWorkItem(ctx =>
                    {
                        var gridGraph = AstarPath.active.data.gridGraph;
                        if (gridGraph == null) return;

                        // 1️⃣ Najpierw wyczyść kary
                        foreach (var node in gridGraph.nodes)
                        {
                            node.Penalty = 0;
                        }

                        // 2️⃣ Przywróć kary dla Obstacles Soft (tak jak robi to GraphPenaltySetter)
                        GraphPenaltySetter gps = FindObjectOfType<GraphPenaltySetter>();
                        if (gps != null)
                        {
                            foreach (var node in gridGraph.nodes)
                            {
                                Vector3 worldPos = (Vector3)node.position;

                                if (Physics2D.OverlapPoint(worldPos, gps.softMask1))
                                    node.Penalty = (uint)gps.softPenalty1;
                                else if (Physics2D.OverlapPoint(worldPos, gps.softMask2))
                                    node.Penalty = (uint)gps.softPenalty2;
                                else if (Physics2D.OverlapPoint(worldPos, gps.softMask3))
                                    node.Penalty = (uint)gps.softPenalty3;
                            }
                        }

                        // 3️⃣ Dodaj kary za jednostki (graczy)
                        foreach (var node in gridGraph.nodes)
                        {
                            Vector2Int tilePos = WorldToTile((Vector2)(Vector3)node.position);
                            if (OccupiedBy.ContainsKey(tilePos))
                                node.Penalty = 8000; // duża kara – omijaj graczy
                        }

                        ctx.QueueFloodFill(); // aktualizuje dane grafu
                    });

                }
                else
                {
                    // ❗Wejście zablokowane – sprawdź, czy ZJEŻDŻAMY ze STRZAŁKI
                    var currentData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));
                    if (currentData != null && currentData.isArrow)
                    {
                        // zabij jednostkę stojącą na kaflu docelowym i wejdź
                        if (OccupiedBy.TryGetValue(nextTile.Value, out var other) && other != null && other != this)
                        {
                            var hp = other.GetComponent<Health>();
                            if (hp != null) hp.TakeDamage(hp.MaxHP);
                            ReleaseIfOccupiedBy(nextTile.Value, other); // zwolnij wpis
                        }

                        // zajmij kafel „po zabiciu”
                        OccupiedBy[nextTile.Value] = this;

                        Vector2Int enteredDir = nextTile.Value - currentTile;
                        ClearReservationIfOwnedBy(nextTile.Value, this);
                        currentTile = nextTile.Value;
                        lastDirection = enteredDir;

                        if (TileLogicManager.Instance != null)
                            TileLogicManager.Instance.HandlePlayerOnTile(gameObject);

                        nextTile = null;
                        pathIndex++;
                        blockTimer = 0f;
                        repathAttempts = 0;

                        if (pathIndex >= pathTiles.Count)
                        {
                            moving = false;
                            isSliding = false;
                            if (logDebug) Debug.Log($"{name}: Arrived (after arrow-kill) {currentTile}");
                        }
                    }
                    else
                    {
                        // zwykłe kafle – bez agresji, zachowanie jak dotąd
                        if (logDebug) Debug.Log($"{name}: Blocked by another unit → repath.");
                        ClearAllReservationsOwnedBy(this);
                        TryRepath("unit");
                        return;
                    }
                }
            }
        }

        // --- Bezpieczne wyrównanie po zakończeniu ruchu ---
        if (!moving && nextTile == null)
        {
            Vector2 correctCenter = TileToWorld(currentTile);
            float offset = Vector2.Distance(transform.position, correctCenter);
            if (offset > 0.001f)
            {
                transform.position = correctCenter;
            }
        }
    }

    // === ŚCIEŻKI ===
    public void RequestPathTo(Vector3 worldTarget)
    {
        lastGoalWorld = worldTarget; // zapamiętaj nowy cel

        ClearAllReservationsOwnedBy(this);
        nextTile = null;
        moving = false;
        pathTiles.Clear();
        pathIndex = 0;
        repathAttempts = 0;

        // Startuj ścieżkę tylko po jednym grafie (DiagonalGraph)
        seeker.StartPath(transform.position, worldTarget, OnPathComplete);
    }
    private void TryRepath(string reason)
    {
        moving = false;
        repathAttempts++;

        if (repathAttempts > maxRepathAttempts)
        {
            Debug.LogWarning($"{name}: Stopped after too many blocked attempts.");
            return;
        }

        if (logDebug)
            Debug.Log($"{name}: Blocked by {reason} → Repath ({repathAttempts}/{maxRepathAttempts})");

        // przelicz ścieżkę od bieżącej pozycji do zapamiętanego celu
        seeker.StartPath(transform.position, lastGoalWorld, OnPathComplete);
    }

    private void OnPathComplete(Path p)
    {
        if (p.error || p.vectorPath == null || p.vectorPath.Count == 0)
        {
            moving = false;
            Debug.LogWarning($"{name}: Pathfinding error: {p.errorLog}");
            ClearAllReservationsOwnedBy(this);
            return;
        }

        var tiles = new List<Vector2Int>(p.vectorPath.Count);
        foreach (var v in p.vectorPath) tiles.Add(WorldToTile(SnapCenter(v)));
        tiles = EnforceDiagonalRules(tiles);

        for (int i = tiles.Count - 2; i >= 0; i--)
            if (tiles[i] == tiles[i + 1]) tiles.RemoveAt(i + 1);

        if (tiles.Count > 0 && tiles[0] == currentTile) tiles.RemoveAt(0);

        pathTiles = tiles;
        pathIndex = 0;
        nextTile = null;
        moving = pathTiles.Count > 0;

        if (isSliding && moving)
        {
            // Blokuj przyjmowanie rozkazów do końca ślizgu
            isSliding = true;
        }


        if (logDebug) Debug.Log($"{name}: New path ({pathTiles.Count} tiles)");
        AstarPath.active.FlushGraphUpdates();
    }

    // === STANY PÓL ===
    private static bool CanReserve(Vector2Int tile, GridMover who)
    {
        if (OccupiedBy.TryGetValue(tile, out var occ) && occ != who) return false;
        if (ReservedBy.TryGetValue(tile, out var res) && res != who) return false;
        return true;
    }

    private static void Reserve(Vector2Int tile, GridMover who) => ReservedBy[tile] = who;

    private static void ClearReservationIfOwnedBy(Vector2Int tile, GridMover who)
    {
        if (ReservedBy.TryGetValue(tile, out var owner) && owner == who)
            ReservedBy.Remove(tile);
    }

    private static void ClearAllReservationsOwnedBy(GridMover who)
    {
        s_tempTiles.Clear();
        foreach (var kv in ReservedBy)
            if (kv.Value == who) s_tempTiles.Add(kv.Key);
        foreach (var t in s_tempTiles) ReservedBy.Remove(t);
        s_tempTiles.Clear();
    }

    private static void Occupy(Vector2Int tile, GridMover who) => OccupiedBy[tile] = who;

    private static void ReleaseIfOccupiedBy(Vector2Int tile, GridMover who)
    {
        if (OccupiedBy.TryGetValue(tile, out var owner) && owner == who)
            OccupiedBy.Remove(tile);
    }

    private bool TryOccupy(Vector2Int tile, GridMover owner)
    {
        if (OccupiedBy.TryGetValue(tile, out var other) && other != owner)
            return false;

        OccupiedBy[tile] = owner;
        return true;
    }

    // === NARZĘDZIA ===
    private static Vector2 SnapCenter(Vector2 worldPos)
    {
        return new Vector2(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f);
    }

    private static Vector3 TileToWorld(Vector2Int tile)
    {
        return new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
    }

    private static Vector2Int WorldToTile(Vector2 world)
    {
        return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
    }

    private static bool IsDiagonalMoveBlocked(Vector2Int from, Vector2Int to)
    {
        // Ruch po prostej nigdy nie jest blokowany
        if (from.x == to.x || from.y == to.y) return false;

        Vector2Int sideA = new(to.x, from.y);
        Vector2Int sideB = new(from.x, to.y);

        var dataA = TileLogicManager.Instance.GetCellDataAt(TileToWorld(sideA));
        var dataB = TileLogicManager.Instance.GetCellDataAt(TileToWorld(sideB));

        bool hardA = dataA != null && dataA.isObstacleHard;
        bool hardB = dataB != null && dataB.isObstacleHard;
        bool softA = dataA != null && dataA.isObstacleSoft;
        bool softB = dataB != null && dataB.isObstacleSoft;

        // 💡 Nowa logika:
        // Blokuj tylko jeśli którakolwiek ze stron to Hard.
        // Jeśli obie są Soft – NIE blokuj.
        if (hardA || hardB)
            return true;
        if (softA && softB)
            return false;

        // 🔹 NIE BLOKUJ, jeśli przeszkodą są tylko jednostki (Playerzy / Enemies)
        bool sideA_unit = OccupiedBy.ContainsKey(sideA);
        bool sideB_unit = OccupiedBy.ContainsKey(sideB);
        if (sideA_unit && sideB_unit)
            return false;

        // Domyślnie brak blokady
        return false;
    }


    private static bool BothSidesSoft(Vector2Int from, Vector2Int to)
    {
        if (from.x == to.x || from.y == to.y) return false; // nie jest ruchem po skosie

        Vector2Int sideA = new(to.x, from.y);
        Vector2Int sideB = new(from.x, to.y);

        var dataA = TileLogicManager.Instance.GetCellDataAt(TileToWorld(sideA));
        var dataB = TileLogicManager.Instance.GetCellDataAt(TileToWorld(sideB));

        bool softA = dataA != null && dataA.isObstacleSoft;
        bool softB = dataB != null && dataB.isObstacleSoft;

        return softA && softB;
    }

    // ======================================================
    // 🔹 OBSŁUGA RUCHU NA POLACH ŚLISKICH
    // ======================================================
    public void SlideForward()
    {
        // brak kierunku = brak ślizgu
        if (lastDirection == Vector2Int.zero)
        {
            isSliding = false;
            return;
        }

        Vector2Int dir = lastDirection;
        Vector2Int probe = currentTile;         // start
        Vector2Int lastValid = currentTile;     // ostatni bezpieczny kafelek

        while (true)
        {
            Vector2Int next = probe + dir;
            Vector3 nextWorld = TileToWorld(next);

            var nextData = TileLogicManager.Instance.GetCellDataAt(nextWorld);
            var currentData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(probe));

            // brak danych → koniec na ostatnim bezpiecznym
            if (nextData == null) break;

            // przeszkody twarde/miękkie lub nieprzechodnie → koniec PRZED nimi
            if (!nextData.walkable || nextData.isObstacleHard || nextData.isObstacleSoft)
                break;

            // zajęty cel → koniec (nie wjeżdżamy na jednostkę)
            if (!CanReserve(next, this))
                break;

            // --- sprawdzenie narożnika dla ruchu po skosie ---
            Vector2Int sideA = new(next.x, probe.y);
            Vector2Int sideB = new(probe.x, next.y);

            var dataA = TileLogicManager.Instance.GetCellDataAt(TileToWorld(sideA));
            var dataB = TileLogicManager.Instance.GetCellDataAt(TileToWorld(sideB));

            bool hardCorner = (dataA != null && dataA.isObstacleHard) || (dataB != null && dataB.isObstacleHard);
            bool bothSoft = (dataA != null && dataA.isObstacleSoft) && (dataB != null && dataB.isObstacleSoft);

            // Hard blokuje, ale dwa Softy już NIE
            if (hardCorner) break;
            if (bothSoft) { /* kontynuuj, nie przerywaj */ }

            // (Soft+Soft już nie blokuje – nic tu nie rób)

            // w pozostałych przypadkach możemy rozważyć ignorowanie blokady:
            bool sideA_unit = OccupiedBy.TryGetValue(sideA, out var moverA) && moverA != null;
            bool sideB_unit = OccupiedBy.TryGetValue(sideB, out var moverB) && moverB != null;
            bool softA = dataA != null && dataA.isObstacleSoft;
            bool softB = dataB != null && dataB.isObstacleSoft;
            bool singleSoft = softA ^ softB;

            // next jest przechodni → zapamiętujemy go jako ostatni bezpieczny
            lastValid = next;

            // jeżeli next nie jest śliski → to nasz cel wyjazdu z lodu
            if (!nextData.isSlippery)
                break;

            // inaczej sondę przesuwamy dalej po lodzie
            probe = next;
        }

        // jeśli nie ma gdzie jechać – koniec ślizgu
        if (lastValid == currentTile)
        {
            isSliding = false;
            return;
        }

        // jedziemy do ustalonego kafelka (ostatni po lodzie lub pierwszy poza nim)
        isSliding = true;
        RequestPathTo(TileToWorld(lastValid));
        moving = true; // utrzymujemy stan ruchu przez cały ślizg
    }

    // ===============================================
    // 🔧 Naprawa ścieżek — blokady diagonalne Hard
    // ===============================================
    private static bool IsTileWalkable(Vector2Int t)
    {
        var d = TileLogicManager.Instance.GetCellDataAt(TileToWorld(t));
        bool blockedByTile = d != null && (d.isObstacleHard || d.isObstacleSoft);
        bool blockedByUnit = OccupiedBy.ContainsKey(t) || ReservedBy.ContainsKey(t);
        return !blockedByTile && !blockedByUnit;
    }

    private static bool IsHard(Vector2Int t)
    {
        var d = TileLogicManager.Instance.GetCellDataAt(TileToWorld(t));
        return d != null && d.isObstacleHard;
    }

    private static List<Vector2Int> EnforceDiagonalRules(List<Vector2Int> raw)
    {
        if (raw == null || raw.Count < 2) return raw;
        var result = new List<Vector2Int> { raw[0] };

        for (int i = 1; i < raw.Count; i++)
        {
            var from = result[result.Count - 1];
            var to = raw[i];

            bool isDiagonal = (from.x != to.x) && (from.y != to.y);
            if (isDiagonal && IsDiagonalMoveBlocked(from, to))
            {
                var sideA = new Vector2Int(to.x, from.y);
                var sideB = new Vector2Int(from.x, to.y);

                if (IsTileWalkable(sideA) && !IsHard(sideA))
                {
                    result.Add(sideA);
                    result.Add(to);
                }
                else if (IsTileWalkable(sideB) && !IsHard(sideB))
                {
                    result.Add(sideB);
                    result.Add(to);
                }
                else
                {
                    if (IsTileWalkable(sideA)) result.Add(sideA);
                    if (IsTileWalkable(sideB)) result.Add(sideB);
                    result.Add(to);
                }
            }
            else
            {
                result.Add(to);
            }
        }
        return result;
    }
    // ======================================================
    // 🧩 FUNKCJE POMOCNICZE DLA STRZAŁEK (Arrow Tiles)
    // ======================================================
    public Vector2Int GetLastDirection()
    {
        return lastDirection;
    }
    public void ForceMove(Vector2Int direction)
    {
        // 🔹 Wymuszenie natychmiastowego ruchu o jedno pole
        Vector2Int next = currentTile + direction;

        // 🔸 Sprawdź, czy ruch jest w granicach mapy (opcjonalne bezpieczeństwo)
        var targetData = TileLogicManager.Instance.GetCellDataAt(new Vector3(next.x + 0.5f, next.y + 0.5f, 0));
        if (targetData == null || !targetData.walkable || targetData.isObstacleHard)
        {
            Debug.Log($"{name}: ArrowTile zablokowany – nie można wejść na {next}");
            return;
        }

        // 🔹 Przygotuj jednoetapową ścieżkę
        pathTiles.Clear();
        pathTiles.Add(next);
        pathIndex = 0;
        nextTile = pathTiles[0];

        // 🔹 Reset flag i liczników ruchu
        moving = true;
        blockTimer = 0f;
        repathAttempts = 0;
        isSliding = false;

        // 🔹 Zapamiętaj kierunek (dla Arrow_Cross)
        lastDirection = direction;

        // 🔹 Debug (dla pewności)
        Debug.Log($"{name}: ForceMove → start ruchu w kierunku {direction}");
    }

    public void SetArrowChain(bool value)
    {
        isOnArrowChain = value;
    }
    public bool IsOnArrowChain()
    {
        return isOnArrowChain;
    }
    public bool IsArrowOnCooldown()
    {
        return arrowMoveCooldown > 0f;
    }
    public Vector2Int GetCurrentTile()
    {
        return currentTile;
    }
}
