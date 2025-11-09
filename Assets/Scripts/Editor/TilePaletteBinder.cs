using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class TilePaletteBinder
{
    // ✅ Powiązania nazw palet z nazwami Tilemap
    private static readonly (string palette, string tilemap)[] bindings = new (string, string)[]
    {
        ("Ground", "Ground"),
        ("Obstacles_Hard", "Obstacles hard"),
        ("Obstacles_Soft", "Obstacles soft"),
        ("Deadly", "Deadly"),
        ("Slippery", "Slippery"),
        ("Bridges", "Bridges"),
        ("Arrows", "Arrows"),
        ("Switchers", "Switchers"),
        ("Other", "Other"),
        ("Decor", "Decor"),
    };

    static TilePaletteBinder()
    {
        // Wywoływane automatycznie po każdej zmianie w edytorze
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        // Pobieramy aktywną paletę
        var palette = GridPaintingState.palette;
        if (palette == null) return;

        string paletteName = palette.name;

        // Szukamy powiązanej Tilemapy
        foreach (var (pal, tilemapName) in bindings)
        {
            if (paletteName == pal)
            {
                var tilemap = GameObject.Find(tilemapName)?.GetComponent<Tilemap>();
                if (tilemap != null && GridPaintingState.scenePaintTarget != tilemap.gameObject)
                {
                    GridPaintingState.scenePaintTarget = tilemap.gameObject;
                    SceneView.RepaintAll();
                    Debug.Log($"🧱 Tilemap automatycznie ustawiona na: {tilemapName}");
                }
                return;
            }
        }
    }
}
