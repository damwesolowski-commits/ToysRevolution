using UnityEngine;
using System.Collections;

public enum BallDirection
{
    Góra,
    Dół,
    Lewo,
    Prawo
}

public class RollingBall : MonoBehaviour
{
    [Header("Prędkość kuli (5 = prędkość jednostki)")]
    [Range(1, 10)]
    public int speedLevel = 5;   // 1 = 1.5s/pole ... 10 = 0.1s/pole
    private float moveDelay;     // czas przejścia o jedno pole (sekundy)

    [Header("Kierunek początkowy")]
    public BallDirection startDirection = BallDirection.Prawo;

    private Vector2Int direction;
    [HideInInspector] public Vector2Int currentTile;

    private TileLogicManager tileLogic;
    private GridMover[] allPlayers;
    private ButtonBase lastButtonTouched = null;

    private void Start()
    {
        tileLogic = TileLogicManager.Instance;
        allPlayers = FindObjectsOfType<GridMover>();

        SetSpeedByLevel();

        // Kierunek startowy
        switch (startDirection)
        {
            case BallDirection.Góra: direction = Vector2Int.up; break;
            case BallDirection.Dół: direction = Vector2Int.down; break;
            case BallDirection.Lewo: direction = Vector2Int.left; break;
            case BallDirection.Prawo: direction = Vector2Int.right; break;
        }

        currentTile = Vector2Int.RoundToInt(transform.position);
        StartCoroutine(MoveRoutine());
    }

    private void SetSpeedByLevel()
    {
        switch (speedLevel)
        {
            case 1: moveDelay = 1.5f; break;
            case 2: moveDelay = 1.2f; break;
            case 3: moveDelay = 1.0f; break;
            case 4: moveDelay = 0.8f; break;
            case 5: moveDelay = 0.6f; break;
            case 6: moveDelay = 0.5f; break;
            case 7: moveDelay = 0.4f; break;
            case 8: moveDelay = 0.3f; break;
            case 9: moveDelay = 0.2f; break;
            case 10: moveDelay = 0.1f; break;
            default: moveDelay = 0.6f; break;
        }
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            if (gameObject == null) yield break; // 🧩 zabezpieczenie po zniszczeniu kuli

            // 🔸 Oblicz aktualną komórkę z lekkim marginesem (eliminuje błędy zaokrągleń)
            Vector3Int currentCell = tileLogic.groundTilemap.WorldToCell(transform.position + new Vector3(0.01f, 0.01f, 0));
            currentTile = (Vector2Int)currentCell;

            Vector2Int nextTile = currentTile + direction;
            // 🔥 Jeśli na docelowym kafelku stoi skrzynia — zniszcz kulę
            if (GridMover.IsCellOccupied(nextTile))
            {
                GridMover occupier = GetOccupier(nextTile);
                if (occupier != null && occupier.moverType == GridMover.GridMoverType.Chest)
                {
                    DestroyBall("Kula wjechała na skrzynię");
                    yield break;
                }
            }

            // 🧩 Zabezpieczenie przed próbą wejścia w ten sam kafel
            if (nextTile == currentTile)
            {
                Debug.Log("⚠️ Kula próbowała ruszyć na ten sam kafel – pominięto cykl.");
                yield return null;
                continue;
            }

            Vector3 nextWorld = tileLogic.groundTilemap.CellToWorld((Vector3Int)nextTile) + new Vector3(0.5f, 0.5f, 0);
            var cell = tileLogic.GetCellDataAt(nextWorld);

            if (cell == null)
            {
                DestroyBall("Poza mapą");
                yield break;
            }

            float radius = GetComponent<CircleCollider2D>()?.radius ?? 0.25f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

            // 🔸 Filtr — usuń z listy wszystkie przyciski, żeby kula nie „uderzała” w nie kolizyjnie
            hits = System.Array.FindAll(hits, h => h.GetComponent<ButtonBase>() == null);

            foreach (var hit in hits)
            {
                // 💥 Kula vs kula
                RollingBall otherBall = hit.GetComponent<RollingBall>();
                if (otherBall != null && hit.gameObject != gameObject)
                {
                    Destroy(hit.gameObject);
                    DestroyBall("Zderzenie z inną kulą");
                    yield break;
                }

                // 💀 Kula vs jednostka
                Health hp = hit.GetComponent<Health>();
                if (hp != null) hp.TakeDamage(hp.MaxHP);
            }

            // 🔹 Wciskanie przycisków – reaguj tylko na zmianę
            ButtonBase closestButton = null;
            float minDistance = float.MaxValue;

            foreach (var button in FindObjectsOfType<ButtonBase>())
            {
                float distance = Vector2.Distance(transform.position, button.transform.position);
                if (distance < minDistance && distance <= radius)
                {
                    minDistance = distance;
                    closestButton = button;
                }
            }

            if (closestButton != null && closestButton != lastButtonTouched)
            {
                if (lastButtonTouched != null)
                    lastButtonTouched.HandleUnitStepOff(gameObject);

                closestButton.HandleUnitStepOn(gameObject);
                lastButtonTouched = closestButton;
            }
            else if (closestButton == null && lastButtonTouched != null)
            {
                lastButtonTouched.HandleUnitStepOff(gameObject);
                lastButtonTouched = null;
            }

            // 🔸 Reakcje na kafelek
            bool deadlyButNotProtected = cell.isDeadly && !tileLogic.IsTileNeutralized(nextWorld);
            if (cell.isObstacleHard || cell.isObstacleSoft || deadlyButNotProtected)
            {
                DestroyBall("Zderzenie z przeszkodą (Hard/Soft) lub deadly polem (bez ochrony mostu)");
                yield break;
            }

            if (cell.isBridge)
            {
                BridgeFalling bridge = FindBridgeAt(nextWorld);
                if (bridge != null)
                    bridge.TriggerCollapse(); // kula może przejechać przed zawaleniem
            }

            if (cell.isArrow && cell.arrowDirection != Vector2Int.zero)
                direction = cell.arrowDirection;

            // 🔸 Ruch (lerp) + obrót
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < moveDelay)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDelay);
                transform.position = Vector3.Lerp(startPos, nextWorld, t);

                float rotationSpeed = 360f / moveDelay;
                float rotDir = (direction == Vector2Int.right || direction == Vector2Int.up) ? -1f : 1f;
                transform.Rotate(Vector3.forward, rotDir * rotationSpeed * Time.deltaTime);

                Collider2D[] midHits = Physics2D.OverlapCircleAll(transform.position, radius);
                if (elapsed < Time.deltaTime * 2f) { yield return null; continue; }

                foreach (var hit in midHits)
                {
                    RollingBall otherBall = hit.GetComponent<RollingBall>();
                    if (otherBall != null && hit.gameObject != gameObject)
                    {
                        Destroy(hit.gameObject);
                        DestroyBall("💥 Zderzenie z inną kulą w ruchu");
                        yield break;
                    }
                }

                yield return null;
            }

            // 🔹 Dodatkowa detekcja logiczna (dla graczy bez fizyki)
            Vector2Int ballTile = Vector2Int.RoundToInt(nextTile);
            foreach (var player in allPlayers)
            {
                if (player == null) continue;
                Vector2Int playerTile = player.GetCurrentTile();
                if (playerTile == ballTile)
                {
                    var hp = player.GetComponent<Health>();
                    if (hp != null)
                    {
                        hp.TakeDamage(hp.MaxHP);
                       // Debug.Log("💥 Kula zabiła gracza logicznie (Tile match).");
                    }
                }
            }

            transform.position = nextWorld;

            // 🔸 Kolizja po ruchu
            Collider2D postHit = Physics2D.OverlapPoint(transform.position);
            if (postHit != null)
            {
                Health hp2 = postHit.GetComponent<Health>();
                if (hp2 != null)
                {
                    hp2.TakeDamage(hp2.MaxHP);
                   // Debug.Log("⚠️ Kula zabiła jednostkę po wejściu na to samo pole (strzałka).");
                }
            }

            // 🔹 Bezpiecznik – kula nie powinna zostać zniszczona i „zawisnąć”
            if (gameObject == null) yield break;
        }
    }

    private BridgeFalling FindBridgeAt(Vector3 worldPos)
    {
        foreach (var bridge in FindObjectsOfType<BridgeFalling>())
        {
            Vector3Int bridgeCell = tileLogic.groundTilemap.WorldToCell(bridge.transform.position);
            Vector3Int targetCell = tileLogic.groundTilemap.WorldToCell(worldPos);
            if (bridgeCell == targetCell)
                return bridge;
        }
        return null;
    }

    private void DestroyBall(string reason)
    {
       // Debug.Log($"💥 Kula zniszczona: {reason}");
        Destroy(gameObject);
    }
    private GridMover GetOccupier(Vector2Int tile)
    {
        // dostęp do prywatnego słownika OccupiedBy w GridMover
        var field = typeof(GridMover).GetField("OccupiedBy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var dict = field.GetValue(null) as System.Collections.IDictionary;

        if (dict.Contains(tile))
            return dict[tile] as GridMover;

        return null;
    }
}
