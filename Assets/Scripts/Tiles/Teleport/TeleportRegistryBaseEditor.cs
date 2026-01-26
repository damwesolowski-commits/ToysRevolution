#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(TeleportRegistryBase), true)]
public class TeleportRegistryBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TeleportRegistryBase reg = (TeleportRegistryBase)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Grupy ID – podgląd i zaznaczanie", EditorStyles.boldLabel);

        if (GUILayout.Button("Odśwież listę"))
        {
            reg.RefreshGroups();
        }

        EditorGUILayout.Space(5);

        foreach (var g in reg.groups.OrderBy(g => g.groupId))
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Grupa ID: {g.groupId}   ({g.count} obiektów)");

            // WYŚWIETLANIE LISTY TELEPORTÓW
            var teleports = reg.GetTeleportsByGroup(g.groupId);

            EditorGUI.indentLevel++;

            foreach (var t in teleports)
            {
                EditorGUILayout.ObjectField(t.name, t, typeof(TeleportBase), true);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Zaznacz wszystkie"))
            {
                reg.SelectAllInGroup(g.groupId);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
    }
}
#endif
