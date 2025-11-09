using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class ColorBlockRegistryLabelsDrawer
{
    static ColorBlockRegistryLabelsDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        // Pobierz kamerę sceny (potrzebna do przeliczania pozycji)
        Camera sceneCam = sceneView.camera;
        if (sceneCam == null)
        {
            Handles.EndGUI();
            return;
        }

        // Pobieramy wszystkie ColorBlock i Buttons
        var blocks = Object.FindObjectsOfType<ColorBlock>();
        var greenButtons = Object.FindObjectsOfType<GreenButton>();
        var redButtons = Object.FindObjectsOfType<RedButton>();

        // Ustawienia stylu tekstu
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        // Rysujemy etykiety nad każdym obiektem
        DrawLabels(blocks, sceneCam, style, Color.white);
        DrawLabels(greenButtons, sceneCam, style, Color.green);
        DrawLabels(redButtons, sceneCam, style, Color.red);

        Handles.EndGUI();
    }

    private static void DrawLabels<T>(T[] objects, Camera cam, GUIStyle style, Color color) where T : Component
    {
        Handles.color = color;
        style.normal.textColor = color;

        foreach (var obj in objects)
        {
            if (obj == null) continue;

            int id = -1;
            Vector3 worldPos = obj.transform.position + Vector3.up * 0.6f;

            if (obj is ColorBlock cb)
                id = cb.groupId;
            else if (obj is GreenButton gb)
                id = gb.groupId;
            else if (obj is RedButton rb)
                id = rb.groupId;

            if (id < 0) continue;

            // Zamiana współrzędnych świata na ekranowe
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue; // za kamerą

            // Rysujemy ID na ekranie
            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            GUI.Label(new Rect(labelPos.x - 10, labelPos.y - 10, 40, 20), id.ToString(), style);
        }
    }
}
