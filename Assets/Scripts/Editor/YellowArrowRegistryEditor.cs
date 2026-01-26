using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(YellowArrowRegistry), true)]
public class YellowArrowRegistryEditor : Editor
{
    private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("🟨 Grupy ID – Żółte strzałki i przyciski", EditorStyles.boldLabel);

        var registry = target as YellowArrowRegistry;
        if (registry == null)
        {
            EditorGUILayout.HelpBox("Brak YellowArrowRegistry.", MessageType.Info);
            return;
        }

        // 1) Zbierz strzałki
        var arrows = FindObjectsByType<YellowArrowBase>(FindObjectsSortMode.None)
                     .Where(a => a != null)
                     .ToList();

        // 2) Zbierz WYŁĄCZNIE żółte przyciski
        var allButtons = FindObjectsByType<ButtonBase>(FindObjectsSortMode.None);
        var buttons = allButtons
                      .Where(b => b != null && b.GetType().Name.Contains("Yellow"))
                      .ToList();

        // 3) Grupowanie po GroupID (wspólne dla strzałek i przycisków)
        var byId = new Dictionary<int, List<GameObject>>();

        void Add<T>(IEnumerable<T> comps, System.Func<T, int> getId) where T : Component
        {
            foreach (var c in comps)
            {
                if (c == null) continue;
                int id = getId(c);
                if (id < 0) continue;

                if (!byId.TryGetValue(id, out var list))
                    byId[id] = list = new List<GameObject>();

                list.Add(c.gameObject);
            }
        }

        Add(arrows, a => a.groupId);
        Add(buttons, b => b.groupId);

        if (byId.Count == 0)
        {
            EditorGUILayout.HelpBox("Nie wykryto żółtych strzałek ani przycisków z Group ID.", MessageType.Info);
            return;
        }

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
                foldouts[id] = true;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            if (foldouts[id])
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < objects.Count; i++)
                {
                    var go = objects[i];

                    string typeName =
                        go.GetComponent<YellowArrowBase>() ? go.GetComponent<YellowArrowBase>().GetType().Name :
                        go.GetComponent<ButtonBase>() ? go.GetComponent<ButtonBase>().GetType().Name :
                        "GameObject";

                    EditorGUILayout.BeginHorizontal();
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
                        FocusSceneView(true);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        if (GUILayout.Button("🔄 Odśwież listę"))
            Repaint();
    }

    private static void SelectAndFrame(List<GameObject> objects)
    {
        Selection.objects = objects.Cast<Object>().ToArray();
        foreach (var o in objects) EditorGUIUtility.PingObject(o);
        FocusSceneView(true);
    }

    private static void FocusSceneView(bool frameSelection = false)
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;
        if (frameSelection) sceneView.FrameSelected();
        sceneView.Repaint();
        SceneView.RepaintAll();
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var stack = new Stack<string>();
        var t = go.transform;
        while (t != null) { stack.Push(t.name); t = t.parent; }
        return string.Join("/", stack);
    }
}
