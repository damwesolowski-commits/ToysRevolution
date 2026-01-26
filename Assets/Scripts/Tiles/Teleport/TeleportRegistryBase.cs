using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class TeleportRegistryBase : MonoBehaviour
{
    protected List<TeleportBase> allTeleports = new List<TeleportBase>();

    public virtual List<TeleportBase> GetAllTeleports()
{
    // Domyślnie: zwróć wszystkie teleporty z każdej grupy (metoda override w pochodnych)
    return new List<TeleportBase>();
}

    // Każdy registry musi to zaimplementować
    public abstract List<TeleportBase> GetTeleportsByGroup(int groupId);

    // --------------------------------------------------------
    //   PODGLĄD PO GRUPACH (jak w TimeBlockRegistry)
    // --------------------------------------------------------

    [System.Serializable]
    public class GroupInfo
    {
        public int groupId;
        public int count;
    }

    public List<GroupInfo> groups = new();

    // 🔵 Odświeżenie listy grup (klikane z Inspektora)
    public void RefreshGroups()
    {
        groups.Clear();

        // bierze tylko teleporty z danego typu registry
        var teles = GetAllTeleports();

        foreach (var t in teles)
        {
            int id = t.groupId;

            var item = groups.FirstOrDefault(g => g.groupId == id);
            if (item == null)
            {
                groups.Add(new GroupInfo()
                {
                    groupId = id,
                    count = 1
                });
            }
            else
            {
                item.count++;
            }
        }
    }


#if UNITY_EDITOR
    public void SelectAllInGroup(int groupId)
    {
        // Pobieramy teleporty tylko obsługiwane przez TEN registry
        var teles = GetTeleportsByGroup(groupId)
                    .Select(t => t.gameObject)
                    .ToArray();

        if (teles.Length == 0)
        {
            Debug.LogWarning($"[TeleportRegistry] Brak teleportów w grupie {groupId}");
            return;
        }

        // Zaznacz w Hierarchii
        UnityEditor.Selection.objects = teles;

        // Przesuń kamerę do pierwszego
        var t = teles[0].transform;
        Bounds b = new Bounds(t.position, Vector3.one * 2f);
        SceneView.lastActiveSceneView.Frame(b, false);
    }
#endif


    // --------------------------------------------------------
    //   LOSOWANIE TELEPORTU DOCELOWEGO
    // --------------------------------------------------------
    public TeleportBase GetRandomTargetTeleport(TeleportBase source)
    {
        var list = GetTeleportsByGroup(source.groupId);
        if (list == null || list.Count == 0) return null;

        List<TeleportBase> allowed = new();

        foreach (var t in list)
        {
            if (t == source) continue;
            if (!t.CanBeExit()) continue;
            allowed.Add(t);
        }

        if (allowed.Count == 0) return null;

        return allowed[Random.Range(0, allowed.Count)];
    }
}
