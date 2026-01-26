using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class YellowArrowRegistry : MonoBehaviour
{
    private List<YellowArrowBase> allArrows = new List<YellowArrowBase>();

    // 🔹 Pobiera wszystkie strzałki o danym ID
    public List<YellowArrowBase> GetArrowsByGroup(int id)
    {
        if (allArrows.Count == 0)
            allArrows.AddRange(FindObjectsByType<YellowArrowBase>(FindObjectsSortMode.None));

        return allArrows.Where(a => a.groupId == id).ToList();
    }

    // 🔹 Przełącza wszystkie strzałki w danej grupie
    public void ToggleGroup(int id)
    {
        var arrows = GetArrowsByGroup(id);
        if (arrows == null || arrows.Count == 0)
        {
            Debug.LogWarning($"[YellowArrowRegistry] Nie znaleziono żadnych strzałek o ID {id}");
            return;
        }

        foreach (var arrow in arrows)
            arrow.ToggleDirection();

        Debug.Log($"[YellowArrowRegistry] Przełączono grupę {id} ({arrows.Count} strzałek)");
    }
}
