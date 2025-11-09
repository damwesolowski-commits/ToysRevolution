using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ColorBlockRegistryBase), true)]
public class ColorBlockRegistryEditor : Editor
{
    // zapamiętujemy, które ID są rozwinięte
    private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("🔍 Grupy ID – podgląd i zaznaczanie", EditorStyles.boldLabel);

        // Zbieramy obiekty z grupami
        var blocks = FindObjectsOfType<ColorBlock>();
        var greenButtons = FindObjectsOfType<GreenButton>();
        var redButtons = FindObjectsOfType<RedButton>();

        // mapujemy: ID -> lista obiektów
        var byId = new Dictionary<int, List<Object>>();

        void Add<T>(IEnumerable<T> comps, System.Func<T, int> getId) where T : Component
        {
            foreach (var c in comps)
            {
                int id = getId(c);
                if (id < 0) continue;
                if (!byId.TryGetValue(id, out var list))
                {
                    list = new List<Object>();
                    byId[id] = list;
                }
                list.Add(c.gameObject);
            }
        }

        Add(blocks, b => b.groupId);
        Add(greenButtons, b => b.groupId);
        Add(redButtons, b => b.groupId);

        if (byId.Count == 0)
        {
            EditorGUILayout.HelpBox("Nie wykryto żadnych obiektów z Group ID.", MessageType.Info);
            return;
        }

        // posortowane ID
        foreach (var kv in byId.OrderBy(k => k.Key))
        {
            int id = kv.Key;
            var objects = kv.Value.Where(o => o != null).Distinct().ToList();

            if (!foldouts.ContainsKey(id)) foldouts[id] = false;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            foldouts[id] = EditorGUILayout.Foldout(
                foldouts[id],
                $"Grupa ID: {id}   ({objects.Count} ob.)",
                true
            );

            if (GUILayout.Button("Zaznacz wszystkie", GUILayout.Width(140)))
            {
                SelectAndFrame(objects);
                Debug.Log($"Zaznaczono {objects.Count} obiektów z grupy ID {id}");
            }

            EditorGUILayout.EndHorizontal();

            if (foldouts[id])
            {
                EditorGUI.indentLevel++;
                // Lista każdego elementu osobno
                for (int i = 0; i < objects.Count; i++)
                {
                    var go = objects[i] as GameObject;
                    if (go == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    // ścieżka w hierarchii + typ
                    string typeName = go.GetComponent<ColorBlock>() ? "ColorBlock"
                                      : go.GetComponent<GreenButton>() ? "GreenButton"
                                      : go.GetComponent<RedButton>() ? "RedButton"
                                      : "GameObject";

                    EditorGUILayout.LabelField($"{i + 1}. [{typeName}]  {GetHierarchyPath(go)}");

                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        EditorGUIUtility.PingObject(go);

                    if (GUILayout.Button("Wybierz", GUILayout.Width(70)))
                    {
                        Selection.activeObject = go;
                        EditorGUIUtility.PingObject(go);
                        FocusSceneView();
                    }

                    if (GUILayout.Button("Pokaż", GUILayout.Width(60)))
                    {
                        Selection.activeObject = go;
                        FocusSceneView(true); // wycentruj kamerę na obiekcie
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        if (GUILayout.Button("🔄 Odśwież listę"))
        {
            // najprostsze odświeżenie: przebudowanie GUI
            Repaint();
        }
    }

    private static void SelectAndFrame(List<Object> objects)
    {
        Selection.objects = objects.ToArray();
        foreach (var o in objects) EditorGUIUtility.PingObject(o);
        FocusSceneView(true);
    }

    private static void FocusSceneView(bool frameSelection = false)
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;
        if (frameSelection)
            sceneView.FrameSelected();
        sceneView.Repaint();
        SceneView.RepaintAll();
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var stack = new Stack<string>();
        var t = go.transform;
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }
}
