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
        var buttons = Object.FindObjectsOfType<ButtonBase>();
        var arrows = Object.FindObjectsOfType<YellowArrowBase>(); // 🟨 strzałki
        var timeButtons = Object.FindObjectsOfType<TimeButton>(); // ⏱️ imeButtony
        var timeBlocks = Object.FindObjectsOfType<TimedColorBlock>();
        var teleports = Object.FindObjectsOfType<TeleportBase>();

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        DrawBlockLabels(blocks, sceneCam, style);
        DrawButtonLabels(buttons, sceneCam, style);
        DrawArrowLabels(arrows, sceneCam, style);
        DrawTimeButtonDurations(timeButtons, sceneCam);
        DrawTimeBlockDurations(timeBlocks, sceneCam);
        DrawTeleportLabels(teleports, sceneCam, style);

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

            string typeName = obj.GetType().Name;
            Color color = typeName.Contains("Green") ? Color.green
                          : typeName.Contains("Red") ? Color.red
                          : typeName.Contains("Yellow") ? Color.yellow
                          : Color.white;

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

    private static void DrawArrowLabels(YellowArrowBase[] arrows, Camera cam, GUIStyle baseStyle)
    {
        foreach (var arrow in arrows)
        {
            if (arrow == null) continue;
            int id = arrow.groupId;
            if (id < 0) continue;

            Color color = new Color(1f, 0.85f, 0.1f);

            Vector3 worldPos = arrow.transform.position + Vector3.up * 0.6f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue;

            var style = new GUIStyle(baseStyle);
            style.normal.textColor = color;
            Handles.color = color;

            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            GUI.Label(new Rect(labelPos.x - 10, labelPos.y - 10, 40, 20), id.ToString(), style);
        }
    }
    private static void DrawTeleportLabels(TeleportBase[] objects, Camera cam, GUIStyle baseStyle)
    {
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            int id = obj.groupId;
            if (id < 0) continue;

            // kolor niebieski dla teleportów
            Color color = new Color(0.2f, 0.6f, 1f);

            Vector3 worldPos = obj.transform.position + Vector3.up * 0.6f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue;

            var style = new GUIStyle(baseStyle);
            style.normal.textColor = color;

            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            GUI.Label(
                new Rect(labelPos.x - 10, labelPos.y - 10, 40, 20),
                id.ToString(),
                style
            );
        }
    }

    // ⏱️ Wyświetlanie sumy trwania najdłuższego cyklu przy TimeButtonie
    private static void DrawTimeButtonDurations(TimeButton[] timeButtons, Camera cam)
    {
        if (timeButtons == null || timeButtons.Length == 0) return;

        foreach (var btn in timeButtons)
        {
            if (btn == null || btn.groupId < 0) continue;

            var registry = Object.FindObjectOfType<TimeBlockRegistry>();
            if (registry == null) continue;

            var blocks = registry.GetBlocksByGroup(btn.groupId)
                                 .OfType<TimedColorBlock>()
                                 .ToList();
            if (blocks.Count == 0) continue;

            float longest = 0f;
            foreach (var b in blocks)
                longest = Mathf.Max(longest, b.GetTotalCycleTime());

            // Pozycja: środek przycisku
            Vector3 worldPos = btn.transform.position;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue;

            string text = $"{longest:0.#}";

            // Styl tekstu: zielony, bez cienia
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.red;

            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            Rect rect = new Rect(labelPos.x - 25, labelPos.y - 10, 60, 20);

            GUI.Label(rect, text, style);
        }
    }

    // 🟩 Wyświetlanie trwania cyklu dla każdego TimeBlocka
    private static void DrawTimeBlockDurations(TimedColorBlock[] blocks, Camera cam)
    {
        if (blocks == null || blocks.Length == 0) return;

        foreach (var block in blocks)
        {
            if (block == null) continue;

            float total = block.GetTotalCycleTime();
            if (total <= 0f) continue;

            // Pozycja: środek bloku
            Vector3 worldPos = block.transform.position;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) continue;

            string text = $"{total:0.#}";

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.red;

            Vector2 labelPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
            Rect rect = new Rect(labelPos.x - 25, labelPos.y - 10, 60, 20);

            GUI.Label(rect, text, style);
        }
    }
}
