using Pathfinding;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static GridData;

public class GridMover : MonoBehaviour
{
    public enum GridMoverType
    {
        Player,
        Enemy,
        Chest
    }
    [Header("Typ obiektu")]
    public GridMoverType moverType = GridMoverType.Player;

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
    private bool atTileCenter = true;   // czy stoimy dokładnie na środku kafelka
    private Vector2Int lastDirection = Vector2Int.zero;
    // 🔒 Blokada przyjmowania nowych rozkazów podczas ślizgania
    private bool isSliding = false;
    // 🔒 Blokada ślizgania się po pchnięciu skrzyni
    private bool justPushedBox = false;
    // 🔄 Zaplanowany ślizg z pola strzałki (wykonywany w następnym FixedUpdate)
    private bool pendingArrowSlide = false;
    private Vector2Int pendingArrowSlideDir = Vector2Int.zero;
    private Vector3? pendingCommand = null;

    public void ScheduleArrowSlide(Vector2Int dir)
    {
        pendingArrowSlide = true;
        pendingArrowSlideDir = dir;
    }

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

        Vector3 pos = transform.position;

        // wymuś ustawienie skrzyni dokładnie na środek kafelka
        Vector2 rounded = SnapCenter(pos);
        transform.position = new Vector3(rounded.x, rounded.y, pos.z);

        // teraz pobierz aktualny tile
        currentTile = WorldToTile(rounded);

        // i ZAJMIJ go w OccupiedBy
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
        // Wszystkie rozkazy ruchu (klik PPM, pogoń za wrogiem itd.)
        // obsługujemy przez ClickToMove2D + RequestPathTo.
        // Tutaj nic nie robimy.
    }

    void FixedUpdate()
    {
        if (arrowMoveCooldown > 0f)
            arrowMoveCooldown -= Time.fixedDeltaTime;

        // 👉 Strzałka stojąca na lodzie mogła zaplanować ślizg
        if (pendingArrowSlide)
        {
            pendingArrowSlide = false;
            lastDirection = pendingArrowSlideDir;
            // Ślizg wystartuje od BIEŻĄCEGO kafla, w kierunku strzałki
            SlideForward();
        }

        if (!moving)
        {
            // Stoimy → jesteśmy na środku kafelka
            // ...ale tylko jeśli nie ma zaplanowanego kroku.
            if (nextTile == null)
                atTileCenter = true;

            // Jeśli stoimy i jest zapamiętany rozkaz – wystartuj nową ścieżkę,
            // ALE tylko wtedy, gdy nie jesteśmy w ślizgu ani na łańcuchu strzałek.
            if (pendingCommand.HasValue)
            {
                // 🔒 Strzałki / śliskie pola mają wyższy priorytet niż pendingCommand.
                // Jeśli jednostka jest w ślizgu lub na łańcuchu strzałek,
                // to zostawiamy pendingCommand na później.
                if (isSliding || isOnArrowChain)
                {
                    if (logDebug)
                        Debug.Log($"{name}: pendingCommand wstrzymany – ślizg / łańcuch strzałek ma priorytet.");
                }
                else
                {
                    Vector3 cmd = pendingCommand.Value;
                    pendingCommand = null;
                    RequestPathTo(cmd);
                }
            }
            return;
        }

        // 1) Rezerwacja następnego kafla
        if (nextTile == null && pathIndex < pathTiles.Count)
        {
            Vector2Int candidate = pathTiles[pathIndex];

            // --- BOX PUSH LOGIC ---
            if (OccupiedBy.TryGetValue(candidate, out var occ) && occ != null)
            {
                var mover = occ.GetComponent<GridMover>();
                if (mover != null && mover.moverType == GridMoverType.Chest)
                {
                    // kierunek ruchu playera
                    Vector2Int direction = candidate - currentTile;

                    // spróbuj popchnąć skrzynię
                    if (!TileLogicManager.Instance.PushBox(occ.gameObject, direction))
                    {
                        // nie da się popchnąć → zatrzymaj playera
                        moving = false;
                        ClearAllReservationsOwnedBy(this);
                        return;
                    }
                    else
                    {
                        justPushedBox = true;
                        // ✅ skrzynia zaczęła się ruszać – zwolnij JEJ STARY kafel,
                        // żeby player mógł wejść na miejsce po skrzyni
                        ReleaseIfOccupiedBy(candidate, mover);
                    }
                }
            }
            // --- END BOX PUSH ---

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


                /// 🧭 Pobieramy dane o kafelku
                var candidateData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(candidate));

                // 🔹 Jeśli nic nie zwrócono – traktujemy jako pole przechodnie
                if (candidateData == null)
                {
                    if (logDebug) Debug.Log($"{name}: candidate {candidate} has no data → treating as walkable");
                }
                else
                {
                    if (moverType == GridMoverType.Chest)
                    {
                        // ⚙️ Dla skrzyni używamy specjalnych zasad:
                        // może wejść na wodę / deadly bez kolców, ale nie na ściany itp.
                        if (!TileLogicManager.Instance.CanChestEnter(candidateData))
                        {
                            ClearAllReservationsOwnedBy(this);
                            TryRepath("CHEST_CANT_ENTER");
                            return;
                        }
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

                        if (!candidateData.walkable && !candidateData.isArrow)
                        {
                            // ✅ wyjątek: woda + koło do pływania
                            if (!CanEnterNonWalkableTile(candidateData, candidate))
                            {
                                ClearAllReservationsOwnedBy(this);
                                TryRepath("NOT WALKABLE");
                                return;
                            }
                        }
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
            // Opuszczamy środek kafla → jesteśmy "pomiędzy"
            atTileCenter = false;

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

                // 🔸 Jeśli na poprzednim kafelku był teleport — powiadom go o zejściu
                if (previousCell != null && previousCell.hasTeleport)
                {
                    previousCell.teleport.HandleUnitStepOff(gameObject);
                }

                if (TryOccupy(nextTile.Value, this))
                {
                    // 🟦 ZAPAMIĘTAJ KIERUNEK zanim podmienisz currentTile
                    Vector2Int enteredDir = nextTile.Value - currentTile;

                    ClearReservationIfOwnedBy(nextTile.Value, this);
                    currentTile = nextTile.Value;
                    lastDirection = enteredDir;

                    // --- JESTEŚMY DOKŁADNIE NA ŚRODKU NOWEGO KAFELKA ---
                    atTileCenter = true;

                    // 🔹 Sprawdź logikę pola (np. kolce, śliskie, strzałki)
                    if (TileLogicManager.Instance != null)
                    {
                        if (moverType == GridMover.GridMoverType.Player)
                        {
                            TileLogicManager.Instance.HandlePlayerOnTile(gameObject);
                        }
                        else
                        {
                            TileLogicManager.Instance.HandleUnitOnTile(gameObject);
                        }
                    }

                    // 🕐 Ustaw cooldown tylko jeśli aktualne pole to strzałka
                    var arrivedCell = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));
                    if (arrivedCell != null && arrivedCell.isArrow)
                        arrowMoveCooldown = 0.05f;
                    else
                        arrowMoveCooldown = 0f;

                    // --- zakończyliśmy krok A→B ---
                    nextTile = null;
                    pathIndex++;
                    blockTimer = 0f;
                    repathAttempts = 0;

                    // 🔚 Czy skończyliśmy starą ścieżkę?
                    bool reachedEndOfCurrentPath = pathIndex >= pathTiles.Count;

                    // --- Po dojściu do środka kafelka sprawdź, czy był zapamiętany rozkaz ---
                    if (pendingCommand.HasValue)
                    {
                        // 🔎 Sprawdź, czy obecny kafelek wymusza ruch (strzałka / lód)
                        bool forcedTile = false;
                        var cellNow = TileLogicManager.Instance != null
                            ? TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile))
                            : null;

                        if (cellNow != null && (cellNow.isArrow || cellNow.isSlippery))
                            forcedTile = true;

                        // Jeżeli:
                        //  - jesteśmy w ślizgu LUB
                        //  - jesteśmy na łańcuchu strzałek LUB
                        //  - stoimy na kafelku wymuszającym ruch,
                        // to NIE wykonujemy jeszcze pendingCommand – zostawiamy go na później.
                        if (isSliding || isOnArrowChain || forcedTile)
                        {
                            if (logDebug)
                                Debug.Log($"{name}: pendingCommand wstrzymany – priorytet ma kafelek (strzałka / lód).");
                        }
                        else
                        {
                            Vector3 cmd = pendingCommand.Value;
                            pendingCommand = null;

                            // 🔄 Kończymy starą ścieżkę i czyścimy stan ruchu
                            moving = false;
                            pathTiles.Clear();
                            pathIndex = 0;
                            nextTile = null;

                            if (logDebug)
                                Debug.Log($"{name}: Arrived {currentTile} (pending command – requesting new path)");

                            // ✅ NOWA ŚCIEŻKA startuje Z TEGO kafelka (na którym właśnie stoimy)
                            InternalRequestPathTo(cmd);

                            // ❗ Nie wchodzimy już w logikę końca starej ścieżki poniżej
                            return;
                        }
                    }

                    // Jeżeli NIE ma zapamiętanego rozkazu, obsługujemy normalne zakończenie ścieżki
                    if (reachedEndOfCurrentPath)
                    {
                        moving = false;

                        if (logDebug) Debug.Log($"{name}: Arrived {currentTile}");

                        // 🔓 Odblokuj sterowanie dopiero po zejściu z lodu
                        var arrivedCell2 = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));

                        // 🔧 Jeśli skrzynia była na innym typie pola niż lód – resetujemy blokadę ślizgu
                        if (arrivedCell2 != null && !arrivedCell2.isSlippery)
                            justPushedBox = false;

                        bool stillOnIce = arrivedCell2 != null && arrivedCell2.isSlippery;
                        if (!stillOnIce)
                            isSliding = false;
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

    // ===============================================
    // 🔹 Ręczne czyszczenie zapamiętanych rozkazów
    //    (używane przy nowym rozkazie gracza)
    // ===============================================
    public void ClearQueuedCommands()
    {
        // zapomnienie ostatniego pendingCommand
        pendingCommand = null;

        // opcjonalnie możesz też zresetować cel repathu:
        lastGoalWorld = transform.position;

        if (logDebug)
            Debug.Log($"{name}: ClearQueuedCommands – wyczyszczono pendingCommand i lastGoalWorld.");
    }

    // === ŚCIEŻKI ===
    public void RequestPathTo(Vector3 worldTarget)
    {
        // 🚫 Jeśli jednostka się ślizga, ignorujemy zewnętrzne rozkazy ruchu
        if (isSliding)
        {
            if (logDebug)
                Debug.Log($"{name}: Ignoruję RequestPathTo – jednostka jest w ślizgu.");
            return;
        }

        // 🚫 Jeśli jednostka jest na łańcuchu strzałek – też ignorujemy zewnętrzne rozkazy
        if (isOnArrowChain)
        {
            if (logDebug)
                Debug.Log($"{name}: Ignoruję RequestPathTo – jednostka jest na łańcuchu strzałek.");
            return;
        }

        // 🔍 Środek aktualnego kafelka
        Vector2 center = TileToWorld(currentTile);
        float distFromCenter = Vector2.Distance(transform.position, center);
        const float centerTolerance = 0.01f;

        // 👉 JEŚLI JESTEŚMY W RUCHU i NIE stoimy dokładnie na środku kafla,
        // to TYLKO buforujemy rozkaz i NIE liczymy teraz nowej ścieżki.
        if (moving && distFromCenter > centerTolerance)
        {
            pendingCommand = worldTarget;

            if (logDebug)
            {
                Debug.Log(
                    $"{name}: RequestPathTo W RUCHU → pendingCommand (dist={distFromCenter:F3})");
            }

            return;
        }

        // 🩹 Jeśli NIE RUSZAMY SIĘ, ale pozycja jest minimalnie „brudna”
        if (!moving && distFromCenter > centerTolerance)
        {
            // 👇 Specjalny przypadek: mamy zaplanowany nextTile, ale moving==false.
            // To oznacza, że jesteśmy w POŁOWIE kroku A→B i ktoś nas zatrzymał.
            // Zgodnie z zasadą: krok A→B musi być niepodzielny.
            if (nextTile != null && !atTileCenter)
            {
                // traktujemy to jak "w trakcie ruchu" → tylko buforujemy rozkaz
                pendingCommand = worldTarget;

                if (logDebug)
                {
                    Debug.Log(
                        $"{name}: RequestPathTo W PÓŁ KROKU (moving==false, nextTile={nextTile}) " +
                        $"→ zapisuję pendingCommand, najpierw dokończę krok.");
                }

                // upewnij się, że dalej jedziemy do nextTile
                moving = true;
                return;
            }

            // zwykły przypadek: stoimy krzywo na środku kafla → dociągamy
            transform.position = center;
            if (logDebug)
                Debug.Log($"{name}: Korekta pozycji → dociągam do środka kafelka przed RequestPathTo.");
        }

        // ✅ Stoimy na środku kafelka → można od razu liczyć ścieżkę
        if (logDebug)
        {
            Debug.Log($"{name}: RequestPathTo NA ŚRODKU kafla → liczę nową ścieżkę");
        }

        pendingCommand = null;
        InternalRequestPathTo(worldTarget);
    }

    // Wewnętrzna wersja – używana np. przez SlideForward, Arrow itp.
    private void InternalRequestPathTo(Vector3 worldTarget)
    {
        atTileCenter = false;

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
        // Jeśli jednostka jest w ślizgu – NIE zmieniamy już kierunku.
        // Zamiast tego przerywamy ślizg w miejscu.
        if (isSliding)
        {
            if (logDebug)
                Debug.Log($"{name}: Blokada w ślizgu ({reason}) → przerywam ślizg bez repathu.");
            moving = false;
            ClearAllReservationsOwnedBy(this);
            isSliding = false;
            return;
        }

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

        // 1) Konwersja ścieżki na kafelki
        var tiles = new List<Vector2Int>(p.vectorPath.Count);
        foreach (var v in p.vectorPath)
            tiles.Add(WorldToTile(SnapCenter(v)));

        // 2) Reguły diagonalne
        tiles = EnforceDiagonalRules(tiles);

        // 3) Usuń duplikaty następujące po sobie
        for (int i = tiles.Count - 2; i >= 0; i--)
        {
            if (tiles[i] == tiles[i + 1])
                tiles.RemoveAt(i + 1);
        }

        // 4) Usuń z początku ścieżki aktualny kafelek (stoimy już na nim)
        while (tiles.Count > 0 && tiles[0] == currentTile)
            tiles.RemoveAt(0);

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

    private static void Occupy(Vector2Int tile, GridMover who)
    {
        // 1) Zapisz, że kafelek jest zajęty
        OccupiedBy[tile] = who;

        // 2) Przelicz karę ruchu w grafie A* dla tego pola
        if (GraphPenaltySetter.Instance != null)
        {
            Vector3 worldPos = TileToWorld(tile);
            GraphPenaltySetter.Instance.RefreshPenaltyAtPosition(worldPos);
        }
    }
    private static void ReleaseIfOccupiedBy(Vector2Int tile, GridMover who)
    {
        if (OccupiedBy.TryGetValue(tile, out var owner) && owner == who)
        {
            // 1) Usuń z mapy zajętości
            OccupiedBy.Remove(tile);

            // 2) Przelicz karę ruchu w grafie A* dla tego pola
            if (GraphPenaltySetter.Instance != null)
            {
                Vector3 worldPos = TileToWorld(tile);
                GraphPenaltySetter.Instance.RefreshPenaltyAtPosition(worldPos);
            }
        }
    }

    private bool TryOccupy(Vector2Int tile, GridMover owner)
    {
        // Jeśli ktoś już stoi na tym polu i to NIE my → nie wchodzimy
        if (OccupiedBy.TryGetValue(tile, out var other) && other != owner)
            return false;

        // Zajmujemy kafelek
        OccupiedBy[tile] = owner;

        // 🔄 PRZELICZ KARĘ RUCHU dla tego pola w grafie A*
        if (GraphPenaltySetter.Instance != null)
        {
            Vector3 worldPos = TileToWorld(tile);
            GraphPenaltySetter.Instance.RefreshPenaltyAtPosition(worldPos);
        }

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
    private bool CanEnterNonWalkableTile(CellData cell, Vector2Int tile)
    {
        // tylko Player/Enemy (skrzynia ma swoje zasady)
        if (moverType == GridMoverType.Chest) return false;

        // Jeśli to WODA i mamy koło (float) → można wejść
        var inv = GetComponent<UnitInventory>();
        if (inv != null && inv.HasFloatEquipped)
        {
            if (TileLogicManager.Instance != null && TileLogicManager.Instance.IsWaterTile(tile))
                return true;
        }

        return false;
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

        // 🧹 Rozpoczęcie ślizgu kasuje wszystkie wcześniejsze rozkazy
        pendingCommand = null;

        // --- SPECJALNA BLOKADA ŚLIZGANIA DLA PLAYERA ---
        if (moverType == GridMoverType.Player)
        {
            Vector2Int next = currentTile + lastDirection;
            var nextData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(next));

            // Jeśli pierwszy krok ślizgu jest zablokowany → nie ślizgamy się
            if (nextData == null ||
                (!nextData.walkable && !CanEnterNonWalkableTile(nextData, next)) ||
                nextData.isObstacleHard ||
                nextData.isObstacleSoft ||
                !CanReserve(next, this))
            {
                isSliding = false;
                return;
            }
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
            if ((!nextData.walkable && !CanEnterNonWalkableTile(nextData, next)) || nextData.isObstacleHard || nextData.isObstacleSoft)
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

            // 🔒 Specjalny zakaz dla skrzyni — skrzynia NIE MOŻE wejść na "walkable", które nie jest jej dozwolone
            if (moverType == GridMoverType.Chest)
            {
                if (!TileLogicManager.Instance.CanChestEnter(nextData))
                {
                    break; // zatrzymaj ślizg przed wejściem na niedozwolony kafel
                }
            }

            // next jest przechodni → zapamiętujemy go jako ostatni bezpieczny
            lastValid = next;

            // jeżeli next nie jest śliski - OBSŁUGA ŚLIZGU → różne zasady dla Player i Chest
            if (!nextData.isSlippery)
            {
                if (moverType == GridMoverType.Chest)
                {
                    // ❌ skrzynia NIE MOŻE wejść na to pole — koniec ślizgu
                    if (!TileLogicManager.Instance.CanChestEnter(nextData))
                    {
                        // zostajemy na ostatnim "legalnym" slipper
                        break;
                    }
                }

                // ✔️ tutaj wiemy, że skrzynia MOŻE wejść
                lastValid = next;
                break;
            }

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
        atTileCenter = false;
        InternalRequestPathTo(TileToWorld(lastValid));
        moving = true;
    }
    public bool HasJustPushedBox()
    {
        bool value = justPushedBox;
        justPushedBox = false;
        return value;
    }
    public void MarkJustPushedBox()
    {
        justPushedBox = true;
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
        // 🧹 Ruch wymuszony przez strzałkę kasuje wcześniejsze rozkazy
        pendingCommand = null;

        // 🔹 Wymuszenie natychmiastowego ruchu o jedno pole
        Vector2Int next = currentTile + direction;

        // 🔸 Sprawdź kafelek docelowy w logice siatki
        var targetData = TileLogicManager.Instance.GetCellDataAt(
            new Vector3(next.x + 0.5f, next.y + 0.5f, 0));

        bool blocked = false;

        if (targetData == null)
        {
            blocked = true;
        }
        else if (moverType == GridMoverType.Chest)
        {
            // Skrzynia korzysta z własnych zasad (może wejść np. na wodę)
            if (!TileLogicManager.Instance.CanChestEnter(targetData))
                blocked = true;
        }
        else
        {
            // Player / Enemy – klasyczne zasady
            if ((!targetData.walkable && !CanEnterNonWalkableTile(targetData, next)) || targetData.isObstacleHard)
                blocked = true;
        }

        if (blocked)
        {
            Debug.Log($"{name}: ForceMove zablokowany – nie można wejść na {next}");
            return;
        }

        // ✅ Jeśli nie ma blokady – ustaw prostą ścieżkę na JEDEN kafelek
        ClearAllReservationsOwnedBy(this);
        nextTile = null;
        pathTiles.Clear();
        pathIndex = 0;
        repathAttempts = 0;

        pathTiles.Add(next);
        moving = true;
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

    // ===============================================
    // 🔹 Zatrzymanie ruchu z zewnątrz (np. przy zmianie rozkazu)
    // ===============================================
    public void StopMoving()
    {
        // ❌ Podczas ślizgu lub jazdy po łańcuchu strzałek
        // ignorujemy próby zatrzymania z zewnątrz.
        if (isSliding || isOnArrowChain)
        {
            if (logDebug)
                Debug.Log($"{name}: StopMoving zignorowane – jednostka ślizga się / jest na łańcuchu strzałek.");
            return;
        }

        // 🧱 KLUCZOWA ZMIANA:
        // Jeśli jesteśmy w trakcie kroku A→B (nextTile != null i NIE jesteśmy w centrum kafla),
        // to NIE przerywamy ruchu w połowie.
        // Po prostu czyścimy dalszą ścieżkę i pozwalamy dokończyć dojście do B.
        if (nextTile != null && !atTileCenter)
        {
            pathTiles.Clear();
            pathIndex = 0;
            // po dojściu do B reachedEndOfCurrentPath będzie true → moving=false samo się ustawi
            if (logDebug)
                Debug.Log($"{name}: StopMoving odroczone – dokończę krok do {nextTile.Value} i zatrzymam się na nim.");
            return;
        }

        // klasyczne zatrzymanie (stoimy na kafelku)
        ClearAllReservationsOwnedBy(this);
        nextTile = null;
        moving = false;
        pathTiles.Clear();
        pathIndex = 0;
    }

    // ======================================================
    // 🔄 TELEPORTACJA – używane przez TeleportEntry
    // ======================================================
    public void TeleportTo(Vector3 worldPos)
    {
        // 1) Zwolnij stary kafelek w systemie OccupiedBy
        ReleaseIfOccupiedBy(currentTile, this);

        // 2) Przeskocz na nowy środek kafelka
        Vector2 snapped = SnapCenter(worldPos);
        transform.position = snapped;

        // 3) Zaktualizuj współrzędne kafelka
        currentTile = WorldToTile(snapped);

        // 🔥 Najpierw sprawdź: czy na polu stoi skrzynia?
        GridMover previousOccupier = null;
        OccupiedBy.TryGetValue(currentTile, out previousOccupier);

        // Jeżeli Player wylądował na skrzyni → natychmiast ginie
        if (moverType == GridMoverType.Player && previousOccupier != null)
        {
            if (previousOccupier.moverType == GridMoverType.Chest)
            {
                var hp = GetComponent<Health>();
                if (hp != null) hp.TakeDamage(hp.MaxHP);
                return;
            }
        }

        // 4) Zajmij nowy kafelek
        Occupy(currentTile, this);

        // 5) Wyczyść ścieżkę i stany ruchu
        pathTiles.Clear();
        pathIndex = 0;
        nextTile = null;
        moving = false;

        // 🧊 Po teleportacji NIE ma ślizgu,
        // a poprzedni kierunek ruchu jest zapominany
        isSliding = false;
        lastDirection = Vector2Int.zero;

        // 🧹 Skasuj też wszystkie wcześniej zapamiętane rozkazy
        pendingCommand = null;

        // po teleportacji stoimy IDEALNIE w środku kafelka
        atTileCenter = true;

        if (TileLogicManager.Instance != null)
        {
            var currentCellData = TileLogicManager.Instance.GetCellDataAt(TileToWorld(currentTile));

            // 🌀 TELEPORT — obsługa wejścia
            if (currentCellData != null && currentCellData.hasTeleport)
            {
                currentCellData.teleport.HandleUnitStepOn(gameObject);
            }

            // 🔹 standardowa logika pól
            TileLogicManager.Instance.HandlePlayerOnTile(gameObject);
        }
    }
    public static bool IsCellOccupied(Vector2Int tile)
    {
        return OccupiedBy.ContainsKey(tile);
    }
}