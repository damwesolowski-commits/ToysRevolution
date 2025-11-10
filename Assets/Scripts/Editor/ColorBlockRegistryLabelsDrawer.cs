using UnityEngine;
using UnityEditor;
using System.Linq;

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

        Camera sceneCam = sceneView.camera;
        if (sceneCam == null)
        {
            Handles.EndGUI();
            return;
        }

        var blocks = Object.FindObjectsOfType<ColorBlock>();
        var buttons = Object.FindObjectsOfType<ButtonBase>(); // <-- wszystkie typy przycisków

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        DrawBlockLabels(blocks, sceneCam, style);
        DrawButtonLabels(buttons, sceneCam, style);

        Handles.EndGUI();
    }

    private static void DrawBlockLabels(ColorBlock[] objects, Camera cam, GUIStyle baseStyle)
    {
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            int id = obj.groupId;
            if (id < 0) continue;

            Vector3 worldPos = obj.transform.position + Vector3.up * 0.6f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue;

            var style = new GUIStyle(baseStyle);
            style.normal.textColor = Color.white;

            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            GUI.Label(new Rect(labelPos.x - 10, labelPos.y - 10, 40, 20), id.ToString(), style);
        }
    }

    private static void DrawButtonLabels(ButtonBase[] objects, Camera cam, GUIStyle baseStyle)
    {
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            int id = obj.groupId;
            if (id < 0) continue;

            // Kolor wg nazwy klasy (Green... / Red...)
            var typeName = obj.GetType().Name;
            Color color = typeName.Contains("Green") ? Color.green
                          : typeName.Contains("Red") ? Color.red
                          : Color.yellow;

            Vector3 worldPos = obj.transform.position + Vector3.up * 0.6f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue;

            var style = new GUIStyle(baseStyle);
            style.normal.textColor = color;
            Handles.color = color;

            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            GUI.Label(new Rect(labelPos.x - 10, labelPos.y - 10, 40, 20), id.ToString(), style);
        }
    }
}
