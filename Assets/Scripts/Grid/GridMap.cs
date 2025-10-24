using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMap : MonoBehaviour
{
    [Header("Tilemapy")]
    public Tilemap groundTilemap;       // kafelki po których można chodzić
    public Tilemap obstacleTilemap;     // kafelki blokujące ruch

    [Header("Dane logiki mapy")]
    public GridData gridData;           // przypisany plik GridData.asset

    [Header("Debug - wizualizacja zajętości")]
    public bool showDebugColors = true; // włącz / wyłącz kolorowanie kafli

    // ==========================
    //  SYSTEM ZAJĘTOŚCI / REZERWACJI
    // ==========================

    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();

    // rezerwacje z właścicielem (np. PlayerController)
    private Dictionary<Vector3Int, Object> reservedBy = new Dictionary<Vector3Int, Object>();

    // ==========================
    //  LOGIKA MAPY
    // ==========================

    void Awake()
    {
        if (gridData == null)
            Debug.LogError("❌ Brak przypisanego GridData! Upewnij się, że bake został wykonany.");
        else
            Debug.Log($"✅ Wczytano GridData o rozmiarze: {gridData.size.x}x{gridData.size.y}");
    }

    public bool IsWalkable(Vector2Int gridPos)
    {
        if (gridData == null) return false;
        if (gridData.TryGetCell(gridPos, out var cell))
            return cell.walkable;
        return false;
    }

    // ==========================
    //  FUNKCJE ZAJĘTOŚCI
    // ==========================

    public bool IsTileOccupied(Vector3Int cell) => occupiedTiles.Contains(cell);

    public void SetTileOccupied(Vector3Int cell, bool occupied)
    {
        if (occupied) occupiedTiles.Add(cell);
        else occupiedTiles.Remove(cell);
        UpdateDebugColors();
    }

    // ==========================
    //  FUNKCJE REZERWACJI
    // ==========================

    public bool TryReserveTile(Vector3Int cell, Object owner)
    {
        if (occupiedTiles.Contains(cell)) return false;
        if (reservedBy.TryGetValue(cell, out var existing) && existing != owner)
            return false;

        reservedBy[cell] = owner;
        UpdateDebugColors();
        return true;
    }

    public void UnreserveTile(Vector3Int cell, Object owner)
    {
        if (reservedBy.TryGetValue(cell, out var existing) && existing == owner)
        {
            reservedBy.Remove(cell);
            UpdateDebugColors();
        }
    }

    public bool IsTileReserved(Vector3Int cell) => reservedBy.ContainsKey(cell);
    public bool IsTileReservedByOther(Vector3Int cell, Object owner)
        => reservedBy.TryGetValue(cell, out var existing) && existing != owner;

    // ==========================
    //  DEBUG WIZUALIZACJA
    // ==========================

    private void UpdateDebugColors()
    {
        if (!showDebugColors || groundTilemap == null) return;

        foreach (var cell in groundTilemap.cellBounds.allPositionsWithin)
        {
            if (!groundTilemap.HasTile(cell)) continue;
            groundTilemap.SetTileFlags(cell, TileFlags.None);
            groundTilemap.SetColor(cell, Color.white);
        }

        foreach (var cell in occupiedTiles)
        {
            if (groundTilemap.HasTile(cell))
                groundTilemap.SetColor(cell, new Color(1f, 0.3f, 0.3f, 1f)); // czerwony
        }

        foreach (var kv in reservedBy)
        {
            var cell = kv.Key;
            if (groundTilemap.HasTile(cell) && !occupiedTiles.Contains(cell))
                groundTilemap.SetColor(cell, new Color(1f, 1f, 0.2f, 1f)); // żółty
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugColors || groundTilemap == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        foreach (var cell in occupiedTiles)
            Gizmos.DrawCube(groundTilemap.GetCellCenterWorld(cell), Vector3.one * 0.9f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        foreach (var cell in reservedBy.Keys)
            Gizmos.DrawWireCube(groundTilemap.GetCellCenterWorld(cell), Vector3.one * 0.9f);
    }
}
