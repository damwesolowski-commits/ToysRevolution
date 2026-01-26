using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

public class GridBakeEditor : EditorWindow
{
    [MenuItem("Tools/Bake Grid Data")]
    public static void BakeGridData()
    {
        Tilemap[] tilemaps = Object.FindObjectsOfType<Tilemap>();
        if (tilemaps.Length == 0)
        {
            Debug.LogWarning("❌ Nie znaleziono żadnej Tilemapy w scenie!");
            return;
        }

        // Tworzymy nowy obiekt GridData
        GridData gridData = ScriptableObject.CreateInstance<GridData>();
        gridData.cells = new Dictionary<Vector2Int, GridData.CellData>();

        int totalTiles = 0;
        int walkable = 0, obstaclesHard = 0, obstaclesSoft = 0, deadly = 0, slippery = 0, bridges = 0, switches = 0, other = 0;

        foreach (var tilemap in tilemaps)
        {
            string name = tilemap.name.ToLower();

            bool isGround = name.Contains("ground");
            bool isObstacleHard = name.Contains("obstacles hard");
            bool isObstacleSoft = name.Contains("obstacles soft");
            bool isDeadly = name.Contains("deadly");
            bool isSlippery = name.Contains("slippery");
            bool isBridge = name.Contains("bridges");
            bool isSwitch = name.Contains("switches");
            bool isOther = name.Contains("other");
            bool isDecor = name.Contains("decor");
            bool isBuildField = name.Contains("build");

            if (isDecor)
                continue; // pomijamy czysto wizualne warstwy

            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(pos);
                if (tile == null) continue;

                totalTiles++;

                Vector2Int cellPos = (Vector2Int)(Vector3Int)pos;
                if (!gridData.cells.TryGetValue(cellPos, out var cell))
                    cell = new GridData.CellData();

                cell.position = cellPos;

                // Ustawienia bazowe
                cell.walkable = isGround;
                cell.cost = 1f;

                // Obstacles hard
                if (isObstacleHard)
                {
                    cell.walkable = false;
                    cell.cost = 9999f;
                    cell.isObstacleHard = true;
                    obstaclesHard++;
                }

                // Obstacles soft
                if (isObstacleSoft)
                {
                    cell.walkable = false;
                    cell.isObstacleSoft = true;
                    cell.cost = 5f;
                    obstaclesSoft++;
                }

                // Deadly
                if (isDeadly)
                {
                    cell.walkable = true;
                    cell.isDeadly = true;
                    deadly++;
                }

                // Slippery
                if (isSlippery)
                {
                    cell.walkable = true;
                    cell.isSlippery = true;
                    slippery++;
                }

                // Bridges
                if (isBridge)
                {
                    cell.walkable = true;
                    cell.isBridge = true;
                    bridges++;
                }

                // Switches
                if (isSwitch)
                {
                    cell.walkable = true;
                    cell.isSwitch = true;
                    switches++;
                }

                // Other
                if (isOther)
                {
                    cell.walkable = true;
                    cell.isSpecial = true;
                    other++;
                }

                // Ground
                if (isGround)
                {
                    cell.walkable = true;
                    walkable++;
                }

                // Build Fields
                if (isBuildField)
                {
                    cell.walkable = true;
                    cell.isBuildField = true;
                }

                // Rezerwacja dla przyszłych Arrows (jeszcze bez implementacji)
                if (name.Contains("arrows"))
                {
                    cell.isArrow = true;
                    // Przyszłościowo: cell.arrowDirection = new Vector2Int(...);
                }

                gridData.SetCell(cellPos, cell);
            }
        }

        var bounds = new BoundsInt();
        bool first = true;
        foreach (var t in tilemaps)
        {
            if (first) { bounds = t.cellBounds; first = false; }
            Vector3Int min = Vector3Int.Min(bounds.position, t.cellBounds.position);
            Vector3Int max = Vector3Int.Max(bounds.position + bounds.size, t.cellBounds.position + t.cellBounds.size);
            bounds.position = min;
            bounds.size = max - min;
        }
        gridData.size = (Vector2Int)bounds.size;

        // Zapis pliku
        string dir = "Assets/Data";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = $"{dir}/GridData.asset";

        var existing = AssetDatabase.LoadAssetAtPath<GridData>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(gridData, existing);
            Debug.Log($"✅ Zaktualizowano istniejący GridData.asset ({totalTiles} pól)");
        }
        else
        {
            AssetDatabase.CreateAsset(gridData, path);
            Debug.Log($"✅ Utworzono nowy GridData.asset ({totalTiles} pól)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"--- 🧱 GRID BAKE SUMMARY ---\n" +
                  $"Walkable: {walkable}\n" +
                  $"Obstacles Hard: {obstaclesHard}\n" +
                  $"Obstacles Soft: {obstaclesSoft}\n" +
                  $"Deadly: {deadly}\n" +
                  $"Slippery: {slippery}\n" +
                  $"Bridges: {bridges}\n" +
                  $"Switches: {switches}\n" +
                  $"Other: {other}\n" +
                  $"Total tiles processed: {totalTiles}");
    }
}
