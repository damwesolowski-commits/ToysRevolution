using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridBakeEditor : EditorWindow
{
    [MenuItem("Tools/Bake GridData")]
    public static void ShowWindow()
    {
        GetWindow<GridBakeEditor>("Bake GridData");
    }

    GridMap gridMap;

    void OnGUI()
    {
        GUILayout.Label("Bake Grid Data", EditorStyles.boldLabel);
        gridMap = (GridMap)EditorGUILayout.ObjectField("Grid Map", gridMap, typeof(GridMap), true);

        if (GUILayout.Button("Bake Grid Data"))
        {
            if (gridMap == null)
            {
                Debug.LogWarning("Brak przypisanego GridMap!");
                return;
            }
            Bake(gridMap);
        }
    }

    void Bake(GridMap grid)
    {
        var ground = grid.groundTilemap;
        var obstacles = grid.obstacleTilemap;

        if (ground == null)
        {
            Debug.LogError("Brak przypisanej groundTilemap!");
            return;
        }

        var bounds = ground.cellBounds;
        var gridData = ScriptableObject.CreateInstance<GridData>();
        gridData.size = new Vector2Int(bounds.size.x, bounds.size.y);

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!ground.HasTile(pos))
                continue;

            var data = new GridData.CellData();
            var obstacleTile = obstacles != null ? obstacles.GetTile(pos) : null;

            // Zapisujemy tylko informację o przechodniości
            data.walkable = obstacleTile == null;

            gridData.SetCell(new Vector2Int(pos.x, pos.y), data);
        }

        string path = "Assets/ScriptableObjects/GridData.asset";

        // Jeśli asset już istnieje, nadpisujemy go, zamiast tworzyć nowy
        var existingData = AssetDatabase.LoadAssetAtPath<GridData>(path);
        if (existingData != null)
        {
            EditorUtility.CopySerialized(gridData, existingData);
            EditorUtility.SetDirty(existingData);
            Debug.Log("♻️ Nadpisano istniejący GridData.asset");
        }
        else
        {
            AssetDatabase.CreateAsset(gridData, path);
            Debug.Log("🆕 Utworzono nowy GridData.asset");
        }

        AssetDatabase.SaveAssets();

        Debug.Log("✅ GridData baked successfully!");
        Debug.Log($"Wygenerowano {gridData.cells.Count} pól w GridData.");

        // Automatycznie przypisujemy GridData do GridMap w scenie
        if (grid != null)
        {
            grid.gridData = gridData;
            EditorUtility.SetDirty(grid);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            Debug.Log("✅ GridData automatycznie przypisane do GridMap!");
        }
    }
}

[InitializeOnLoad]
public static class AutoGridBake
{
    static AutoGridBake()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Uruchamiamy bake automatycznie przed startem gry
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var gridMap = Object.FindFirstObjectByType<GridMap>();
            if (gridMap != null)
            {
                Debug.Log("🔄 Auto-bake GridData przed uruchomieniem gry...");
                BakeNow(gridMap);
            }
        }
    }

    private static void BakeNow(GridMap grid)
    {
        var ground = grid.groundTilemap;
        var obstacles = grid.obstacleTilemap;

        if (ground == null)
        {
            Debug.LogWarning("⚠️ Auto-bake: Brak przypisanej groundTilemap.");
            return;
        }

        var bounds = ground.cellBounds;
        var gridData = ScriptableObject.CreateInstance<GridData>();
        gridData.size = new Vector2Int(bounds.size.x, bounds.size.y);

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!ground.HasTile(pos))
                continue;

            var data = new GridData.CellData();
            var obstacleTile = obstacles != null ? obstacles.GetTile(pos) : null;

            data.walkable = obstacleTile == null;
            gridData.SetCell(new Vector2Int(pos.x, pos.y), data);
        }

        var existingData = AssetDatabase.LoadAssetAtPath<GridData>("Assets/ScriptableObjects/GridData.asset");
        if (existingData != null)
        {
            EditorUtility.CopySerialized(gridData, existingData);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(gridData, "Assets/ScriptableObjects/GridData.asset");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Auto-bake zakończony pomyślnie!");
    }
}
