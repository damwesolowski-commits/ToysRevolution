using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(SecretButton_RuleTile))]
public class SecretButton_RuleTile_Editor : Editor
{
    void OnSceneGUI()
    {
        SecretButton_RuleTile button = (SecretButton_RuleTile)target;
        if (button == null || button.secretTiles == null) return;

        Event e = Event.current;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        // 🔹 Klik ALT+LPM -> dodaj nowy wpis do listy
        if (e.type == EventType.MouseDown && e.alt && e.button == 0)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPos = worldRay.origin;
            Vector3Int cell = Vector3Int.zero;

            // Znajdź najbliższą tilemapę w scenie
            Tilemap targetTilemap = null;
            foreach (var tm in FindObjectsOfType<Tilemap>())
            {
                if (tm.GetComponent<TilemapRenderer>() == null) continue;
                cell = tm.WorldToCell(worldPos);
                if (tm.HasTile(cell))
                {
                    targetTilemap = tm;
                    break;
                }
            }

            if (targetTilemap != null)
            {
                Undo.RecordObject(button, "Add Secret Tile");

                var newData = new SecretTileData
                {
                    targetTilemap = targetTilemap,
                    position = cell,
                    tileToPlace = targetTilemap.GetTile(cell),
                    removeOnActivate = true,
                    startDelay = 0f,
                    effectDuration = 5f
                };

                button.secretTiles.Add(newData);
                EditorUtility.SetDirty(button);
                Debug.Log($"➕ Dodano SecretTile na {cell} (Tilemap: {targetTilemap.name})");

                e.Use();
            }
        }

        // 🔹 Rysowanie podświetleń
        for (int i = 0; i < button.secretTiles.Count; i++)
        {
            var data = button.secretTiles[i];
            if (data == null || data.targetTilemap == null) continue;

            Vector3 worldPos = data.targetTilemap.CellToWorld(data.position) + new Vector3(0.5f, 0.5f, 0f);

            // Kolor zależny od akcji
            Color c = data.removeOnActivate ? new Color(1f, 0.2f, 0.2f, 0.3f) : new Color(0.2f, 1f, 0.2f, 0.3f);

            // Kwadrat z wypełnieniem
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[]
                {
                    worldPos + new Vector3(-0.5f, -0.5f, 0),
                    worldPos + new Vector3(-0.5f, 0.5f, 0),
                    worldPos + new Vector3(0.5f, 0.5f, 0),
                    worldPos + new Vector3(0.5f, -0.5f, 0)
                },
                c,
                Color.yellow
            );

            // 🔢 Numer elementu
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            Handles.Label(worldPos, $"{i}", style);

            // 🕓 Wyświetl startDelay i czas trwania
            GUIStyle small = new GUIStyle(EditorStyles.miniLabel);
            small.normal.textColor = Color.cyan;
            small.alignment = TextAnchor.UpperCenter;
            Handles.Label(worldPos + new Vector3(0, 0.45f, 0),
                $"Delay: {data.startDelay:0.0}s | Duration: {data.effectDuration:0.0}s", small);
        }
    }
}
