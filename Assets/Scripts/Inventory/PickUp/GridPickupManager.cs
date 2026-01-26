using System.Collections.Generic;
using UnityEngine;

public class GridPickupManager : MonoBehaviour
{
    public static GridPickupManager Instance { get; private set; }

    private bool hooked = false;

    // Pozwalamy na wiele pickupów na tym samym kafelku (rzadkie, ale bezpieczne)
    private readonly Dictionary<Vector2Int, List<GridPickupBase>> pickupsByTile = new();
    private void Start()
    {
        TryHook();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        TryHook();
    }

    private void OnDisable()
    {
        if (TileLogicManager.Instance != null && hooked)
            TileLogicManager.Instance.OnUnitEnteredTile -= HandleUnitEnteredTile;

        hooked = false;
        CancelInvoke(nameof(TryHook));
    }

    public void Register(GridPickupBase pickup, Vector2Int tile)
    {
        if (pickup == null) return;

        if (!pickupsByTile.TryGetValue(tile, out var list))
        {
            list = new List<GridPickupBase>(1);
            pickupsByTile[tile] = list;
        }

        if (!list.Contains(pickup))
            list.Add(pickup);
    }

    public void Unregister(GridPickupBase pickup, Vector2Int tile)
    {
        if (pickup == null) return;

        if (!pickupsByTile.TryGetValue(tile, out var list)) return;

        list.Remove(pickup);
        if (list.Count == 0)
            pickupsByTile.Remove(tile);
    }

    private void HandleUnitEnteredTile(GameObject unit, Vector2Int tile)
    {
        if (unit == null) return;

        if (!pickupsByTile.TryGetValue(tile, out var list)) return;
        if (list == null || list.Count == 0) return;

        // kopiujemy, bo pickup może się zniszczyć w trakcie
        var temp = ListPool<GridPickupBase>.Get();
        temp.AddRange(list);

        for (int i = 0; i < temp.Count; i++)
        {
            var p = temp[i];
            if (p == null) continue;

            if (p.TryAutoPickup(unit))
            {
                // pickup sam się zwykle niszczy, ale na wszelki wypadek:
                // usunięcie z rejestru odbywa się w OnDestroy/GridPickupBase
            }
        }

        temp.Clear();
        ListPool<GridPickupBase>.Release(temp);
    }
    private void TryHook()
    {
        if (hooked) return;

        if (TileLogicManager.Instance == null)
        {
            Invoke(nameof(TryHook), 0.1f);
            return;
        }

        TileLogicManager.Instance.OnUnitEnteredTile += HandleUnitEnteredTile;
        hooked = true;
        CancelInvoke(nameof(TryHook));
    }

    // Prosty pool na listy, żeby nie generować garbage.
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();

        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(4);

        public static void Release(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            pool.Push(list);
        }
    }
}
