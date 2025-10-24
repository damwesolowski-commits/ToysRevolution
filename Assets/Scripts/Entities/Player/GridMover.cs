using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

/// <summary>
/// Ruch po siatce z A* + system pól: ZAJĘTE / ZAREZERWOWANE.
/// Reguły:
/// - Jednostka stoi na polu = pole jest ZAJĘTE.
/// - Zanim ruszy na kolejny kafel: rezerwuje go (ZAREZERWOWANE).
/// - Po wejściu na kafel: rezerwacja znika, kafel staje się ZAJĘTY.
/// - Na ZAJĘTE ani ZAREZERWOWANE nie może wejść inna jednostka.
/// - Jeżeli ruch zostanie przerwany (stop, zniszczenie, błąd ścieżki), wszystkie rezerwacje tej jednostki są czyszczone.
/// - Kiedy dwa unity dostaną ten sam cel: tylko pierwsze zarezerwuje kafel docelowy; drugie będzie czekać lub spróbuje przeplanować po czasie.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Seeker))]
public class GridMover : MonoBehaviour
{
    // ====== USTAWIENIA ======
    [Header("Ruch")]
    public float tilesPerSecond = 1.666f;     // ~1 kratka / 0.6 s
    public float arriveEps = 0.12f;           // promień uznania wejścia na środek kafla

    [Header("Planowanie / Blokady")]
    public float blockRepathDelay = 0.35f;    // co ile sekund próbować przeplanować, gdy zablokowano następny kafel
    public bool logDebug = false;

    // ====== KOMPONENTY ======
    private Rigidbody2D rb;
    private Seeker seeker;
    private SelectableHighlight highlight;

    // ====== STAN RUCHU ======
    private List<Vector2Int> pathTiles = new List<Vector2Int>(); // ścieżka w postaci kafli
    private int pathIndex = 0;                                   // indeks następnego kafla do rezerwacji
    private bool moving = false;

    private Vector2Int currentTile; // kafel, na którym STOIMY (ZAJĘTE)
    private Vector2Int? nextTile;   // kafel, w który aktualnie IDZIEMY (ZAREZERWOWANE)

    private float blockTimer = 0f;  // licznik do przeplanowania, gdy blokada

    // ====== GLOBALNE MAPY STANÓW PÓL ======
    // Kto zajmuje dany kafel
    private static readonly Dictionary<Vector2Int, GridMover> OccupiedBy = new Dictionary<Vector2Int, GridMover>();
    // Kto zarezerwował dany kafel
    private static readonly Dictionary<Vector2Int, GridMover> ReservedBy = new Dictionary<Vector2Int, GridMover>();

    // ====== UNITY ======
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        seeker = GetComponent<Seeker>();
        highlight = GetComponent<SelectableHighlight>();

        // wyrównaj na siatkę, zarejestruj kafel jako ZAJĘTY
        Vector2 rounded = SnapCenter(rb.position);
        rb.position = rounded;
        currentTile = WorldToTile(rounded);
        Occupy(currentTile, this);
    }

    void OnDisable()
    {
        // na wszelki wypadek — posprzątaj
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
        // Reaguj na PPM tylko jeśli jednostka jest zaznaczona (możesz wyłączyć ten warunek, jeśli sterujesz inaczej)
        if (highlight != null && !highlight.IsSelected) return;

        if (Input.GetMouseButtonDown(1))
        {
            // klik docelowy przyjmujemy z raycastu/transformu kursora ustawianego przez ClickToMove2D
            // Domyślnie ten komponent często podaje "target" jako transform "cursor".
            // Jeśli w Twojej wersji używasz innego sposobu, po prostu wywołaj RequestPathTo(worldPos).
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0;
            RequestPathTo(world);
        }
    }

    void FixedUpdate()
    {
        if (!moving)
            return;

        // Jeżeli nie mamy zarezerwowanego kolejnego kafla — spróbuj zarezerwować
        if (nextTile == null && pathIndex < pathTiles.Count)
        {
            Vector2Int candidate = pathTiles[pathIndex];

            if (CanReserve(candidate, this) && !IsDiagonalMoveBlocked(currentTile, candidate))
            {
                Reserve(candidate, this);
                nextTile = candidate;
                if (logDebug) Debug.Log($"{name}: Reserve {candidate}");
            }
            else
            {
                // zablokowany kafel — czekamy chwilę i/lub przeplanowujemy
                blockTimer += Time.fixedDeltaTime;
                if (blockTimer >= blockRepathDelay)
                {
                    blockTimer = 0f;
                    if (pathTiles.Count > 0)
                    {
                        Vector2Int goal = pathTiles[pathTiles.Count - 1];
                        Vector3 goalWorld = TileToWorld(goal);
                        if (logDebug) Debug.Log($"{name}: Blocked -> Repath to {goal}");
                        RequestPathTo(goalWorld);
                    }
                }
                return;
            }
        }


        // Jeżeli mamy zarezerwowany następny kafel — idziemy w jego środek
        if (nextTile != null)
        {
            Vector2 nextCenter = TileToWorld(nextTile.Value);
            float step = tilesPerSecond * Time.fixedDeltaTime;
            Vector2 newPos = Vector2.MoveTowards(rb.position, nextCenter, step);
            // Jeśli celny kafel leży na murze (ObstaclesHard) → zatrzymaj
            if (Physics2D.OverlapCircle(nextCenter, 0.4f, LayerMask.GetMask("ObstaclesHard")))
            {
                if (logDebug) Debug.LogWarning($"{name}: Hard obstacle at {nextTile.Value}, stopping!");
                moving = false;
                nextTile = null;
                return;
            }

            rb.MovePosition(newPos);

            if (Vector2.Distance(newPos, nextCenter) <= arriveEps)
            {
                // Wejście na kafel:
                // 1) poprzedni kafel przestaje być ZAJĘTY
                // 2) rezerwacja -> staje się ZAJĘTE dla nowego kafla
                // 3) zwolnij starą okupację
                ReleaseIfOccupiedBy(currentTile, this);

                // 4) przenieś status na nowy kafel
                ClearReservationIfOwnedBy(nextTile.Value, this);
                Occupy(nextTile.Value, this);

                // 5) aktualizacja stanu jednostki
                currentTile = nextTile.Value;
                nextTile = null;
                pathIndex++;
                blockTimer = 0f;

                // czy to był koniec ścieżki?
                if (pathIndex >= pathTiles.Count)
                {
                    moving = false;
                    // dla pewności dociągnij dokładnie do środka
                    rb.position = TileToWorld(currentTile);
                    if (logDebug) Debug.Log($"{name}: Arrived {currentTile}");
                }
            }
        }
    }

    // ====== API RUCHU ======
    public void RequestPathTo(Vector3 worldTarget)
    {
        // Wyczyść WSZYSTKO (nie tylko rezerwacje)
        ClearAllReservationsOwnedBy(this);
        nextTile = null;
        moving = false;
        pathTiles.Clear();
        pathIndex = 0;

        // Sprawdź teren docelowy
        bool nearHardObstacle = Physics2D.OverlapCircle(worldTarget, 0.4f, LayerMask.GetMask("ObstaclesHard"));
        bool nearSoftObstacle = Physics2D.OverlapCircle(worldTarget, 0.4f, LayerMask.GetMask("ObstaclesSoft"));

        // Wybierz graf (Orthogonal lub Diagonal)
        GraphMask mask = nearHardObstacle
            ? GraphMask.FromGraphName("OrthogonalGraph")
            : GraphMask.FromGraphName("DiagonalGraph");

        if (logDebug)
            Debug.Log($"{name}: New path request ({(nearHardObstacle ? "Orthogonal" : "Diagonal")})");

        // Uruchom nową ścieżkę
        seeker.StartPath(rb.position, worldTarget, OnPathComplete, mask);
    }

    private void OnPathComplete(Path p)
    {
        if (p.error || p.vectorPath == null || p.vectorPath.Count == 0)
        {
            moving = false;
            if (logDebug) Debug.LogWarning($"{name}: Path error: {p.errorLog}");
            // w razie błędu upewnij się, że nie mamy rezerwacji
            ClearAllReservationsOwnedBy(this);
            return;
        }

        // Zamiana vectorPath na sekwencję kafli
        List<Vector2Int> tiles = new List<Vector2Int>(p.vectorPath.Count);
        for (int i = 0; i < p.vectorPath.Count; i++)
        {
            Vector3 v = p.vectorPath[i];
            tiles.Add(WorldToTile(SnapCenter(v)));
        }

        // Usuń duplikaty następujące po sobie (AA -> A)
        for (int i = tiles.Count - 2; i >= 0; i--)
        {
            if (tiles[i] == tiles[i + 1])
                tiles.RemoveAt(i + 1);
        }

        // Upewnij się, że pierwszy element nie jest bieżącym kaflem
        if (tiles.Count > 0 && tiles[0] == currentTile)
            tiles.RemoveAt(0);

        pathTiles = tiles;
        pathIndex = 0;
        nextTile = null;
        moving = pathTiles.Count > 0;

        if (logDebug)
        {
            string dbg = string.Join("->", pathTiles);
            Debug.Log($"{name}: New path ({pathTiles.Count}): {dbg}");
        }
    }

    // ====== FUNKCJE STANÓW PÓL ======
    private static bool CanReserve(Vector2Int tile, GridMover who)
    {
        // wolne tylko jeśli:
        // - nie ZAJĘTE przez kogoś innego
        // - nie ZAREZERWOWANE przez kogoś innego
        if (OccupiedBy.TryGetValue(tile, out var occ) && occ != who) return false;
        if (ReservedBy.TryGetValue(tile, out var res) && res != who) return false;
        return true;
    }

    private static void Reserve(Vector2Int tile, GridMover who)
    {
        ReservedBy[tile] = who; // nadpisanie własnej rezerwacji jest OK (idempotentne)
    }

    private static void ClearReservationIfOwnedBy(Vector2Int tile, GridMover who)
    {
        if (ReservedBy.TryGetValue(tile, out var owner) && owner == who)
            ReservedBy.Remove(tile);
    }

    private static void ClearAllReservationsOwnedBy(GridMover who)
    {
        // unikanie alokacji: zbierz klucze do listy i dopiero usuń
        s_tempTiles.Clear();
        foreach (var kv in ReservedBy)
            if (kv.Value == who) s_tempTiles.Add(kv.Key);
        for (int i = 0; i < s_tempTiles.Count; i++)
            ReservedBy.Remove(s_tempTiles[i]);
        s_tempTiles.Clear();
    }
    private static readonly List<Vector2Int> s_tempTiles = new List<Vector2Int>(16);

    private static void Occupy(Vector2Int tile, GridMover who)
    {
        OccupiedBy[tile] = who; // jeśli to ten sam, nadpisanie jest OK
    }

    private static void ReleaseIfOccupiedBy(Vector2Int tile, GridMover who)
    {
        if (OccupiedBy.TryGetValue(tile, out var owner) && owner == who)
            OccupiedBy.Remove(tile);
    }

    // ====== NARZĘDZIA KONWERSJI ======
    private static Vector2 SnapCenter(Vector2 worldPos)
    {
        return new Vector2(
            Mathf.Round(worldPos.x - 0.5f) + 0.5f,
            Mathf.Round(worldPos.y - 0.5f) + 0.5f
        );
    }

    private static Vector3 TileToWorld(Vector2Int tile)
    {
        return new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
    }

    private static Vector2Int WorldToTile(Vector2 world)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x - 0.5f), Mathf.RoundToInt(world.y - 0.5f));
    }
    // ====== DODATKOWA OCHRONA PRZED RUCHEM PO SKOSIE ======
    private static bool IsDiagonalMoveBlocked(Vector2Int from, Vector2Int to)
    {
        // jeśli ruch nie jest po skosie — OK
        if (from.x == to.x || from.y == to.y) return false;

        // Sprawdź dwa sąsiadujące kafle po osi X i Y
        Vector2Int sideA = new Vector2Int(to.x, from.y);
        Vector2Int sideB = new Vector2Int(from.x, to.y);

        // Jeśli którykolwiek jest zajęty lub zarezerwowany — zablokuj
        if (OccupiedBy.ContainsKey(sideA) || OccupiedBy.ContainsKey(sideB)) return true;
        if (ReservedBy.ContainsKey(sideA) || ReservedBy.ContainsKey(sideB)) return true;

        return false;
    }
}
