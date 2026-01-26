using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TilePositionViewer : MonoBehaviour
{
    public Tilemap targetTilemap;

#if UNITY_EDITOR
    private Vector3Int lastCellPos;
    private bool hasTile = false;
    private double lastClickTime = 0;

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.beforeSceneGui += DrawOverlay;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.beforeSceneGui -= DrawOverlay;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (targetTilemap == null) return;
        Event e = Event.current;
        if (e == null || !e.shift) return;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPos = ray.origin;

            lastCellPos = targetTilemap.WorldToCell(worldPos);
            hasTile = true;

            // Skopiuj współrzędne do schowka
            GUIUtility.systemCopyBuffer = $"({lastCellPos.x}, {lastCellPos.y}, {lastCellPos.z})";
            Debug.Log($"📍 Kliknięty kafel: {lastCellPos} → skopiowano do schowka ✅");

            lastClickTime = EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
            e.Use();
        }

        // Rysowanie ramki wokół klikniętego kafla
        if (hasTile)
        {
            Vector3 center = targetTilemap.CellToWorld(lastCellPos) + new Vector3(0.5f, 0.5f, 0f);
            Handles.color = Color.yellow;
            Handles.DrawWireCube(center, Vector3.one * 0.95f);
        }
    }

    private void DrawOverlay(SceneView view)
    {
        if (!hasTile) return;

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 40), GUI.skin.box);
        GUILayout.Label($"📍 Wybrany kafel: ({lastCellPos.x}, {lastCellPos.y}, {lastCellPos.z})", EditorStyles.boldLabel);
        GUILayout.Label("Kliknij SHIFT + LPM, aby pobrać współrzędne", EditorStyles.miniLabel);
        GUILayout.EndArea();
        Handles.EndGUI();
    }
#endif
}
