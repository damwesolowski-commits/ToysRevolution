using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ColorBlockRegistryBase), true)]
public class ColorBlockRegistryEditor : Editor
{
    private static Dictionary<int, bool> foldouts = new Dictionary<int, bool>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("🔍 Grupy ID – podgląd i zaznaczanie", EditorStyles.boldLabel);

        var registry = target as ColorBlockRegistryBase;

        var blocks = new List<ColorBlock>();

        if (registry is GreenBlockRegistry)
            blocks.AddRange(FindObjectsByType<GreenBlock>(FindObjectsSortMode.None).Cast<ColorBlock>());
        else if (registry is RedBlockRegistry)
            blocks.AddRange(FindObjectsByType<RedBlock>(FindObjectsSortMode.None).Cast<ColorBlock>());
        else if (registry is BlueBlockRegistry)
            blocks.AddRange(FindObjectsByType<BlueBlock>(FindObjectsSortMode.None).Cast<ColorBlock>());
        else if (registry is BrownBlockRegistry)
            blocks.AddRange(FindObjectsByType<BrownBlock>(FindObjectsSortMode.None).Cast<ColorBlock>());
        else if (registry is GrayBlockRegistry)
            // ⬇️ tu nie odwołujemy się do nieistniejącej klasy GrayBlock – filtr po nazwie
            blocks.AddRange(FindObjectsByType<ColorBlock>(FindObjectsSortMode.None)
                            .Where(b => b != null && b.GetType().Name.Contains("Gray")));
        else if (registry is PinkPurpleBlockRegistry)
            blocks.AddRange(FindObjectsByType<PinkPurpleBlockBase>(FindObjectsSortMode.None).Cast<ColorBlock>());
        else if (registry is TimeBlockRegistry)
            blocks.AddRange(FindObjectsByType<TimedColorBlock>(FindObjectsSortMode.None).Cast<ColorBlock>());
        else
            blocks.AddRange(FindObjectsByType<ColorBlock>(FindObjectsSortMode.None));

        var allButtons = FindObjectsByType<ButtonBase>(FindObjectsSortMode.None);
        var buttons = new List<ButtonBase>();

        // ➜ Zbierz odpowiednie przyciski wg typu rejestru (po nazwie klasy)
        if (registry is GreenBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("Green")));
        else if (registry is RedBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("Red")));
        else if (registry is BlueBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("Blue")));
        else if (registry is BrownBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("Brown")));
        else if (registry is GrayBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("Gray")));
        else if (registry is PinkPurpleBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("PinkPurple")));
        else if (registry is TimeBlockRegistry)
            buttons.AddRange(allButtons.Where(b => b != null && b.GetType().Name.Contains("Time")));
        else
            buttons.AddRange(allButtons);

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

        Add(blocks, b => b.groupId);
        Add(buttons, b => b.groupId);

        if (byId.Count == 0)
        {
            EditorGUILayout.HelpBox("Nie wykryto żadnych obiektów z Group ID.", MessageType.Info);
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
                    string typeName = go.GetComponent<ColorBlock>() ? "ColorBlock"
                                      : go.GetComponent<ButtonBase>() ? go.GetComponent<ButtonBase>().GetType().Name
                                      : "GameObject";

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
        Selection.objects = objects.Cast<UnityEngine.Object>().ToArray();
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
