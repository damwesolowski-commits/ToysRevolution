using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleport_MysteryRegistry : TeleportRegistryBase
{
    public override List<TeleportBase> GetTeleportsByGroup(int groupId)
    {
        allTeleports = FindObjectsByType<TeleportBase>(FindObjectsSortMode.None)
            .Where(t =>
                t is Teleport_MysteryTrigger ||
                t is Teleport_MysteryEntrance ||
                t is Teleport_MysteryExit)
            .ToList();

        return allTeleports
            .Where(t => t.groupId == groupId)
            .ToList();
    }

    public override List<TeleportBase> GetAllTeleports()
    {
        return FindObjectsByType<TeleportBase>(FindObjectsSortMode.None)
            .Where(t =>
                t is Teleport_MysteryTrigger ||
                t is Teleport_MysteryEntrance ||
                t is Teleport_MysteryExit)
            .ToList();
    }

    // 🔹 Pomocnicze – wejścia w danej grupie
    public List<Teleport_MysteryEntrance> GetEntrancesByGroup(int groupId)
    {
        return FindObjectsByType<Teleport_MysteryEntrance>(FindObjectsSortMode.None)
            .Where(e => e.groupId == groupId)
            .ToList();
    }

    // 🔹 Aktywacja wszystkich wejść w grupie (wołana przez Trigger)
    public void ActivateEntrancesForGroup(int groupId, bool playMusic)
    {
        var entrances = GetEntrancesByGroup(groupId);

        if (entrances == null || entrances.Count == 0)
        {
            Debug.Log($"[Teleport_MysteryRegistry] Brak wejść w grupie {groupId} do aktywacji.");
            return;
        }

        foreach (var entrance in entrances)
        {
            entrance.Activate(playMusic);
        }
    }

    // 🔹 Informacja, że jakieś wejście zostało użyte → wyłączamy na stałe wszystkie Triggery w tej grupie
    public void NotifyEntranceUsed(int groupId)
    {
        var triggers = FindObjectsByType<Teleport_MysteryTrigger>(FindObjectsSortMode.None)
            .Where(t => t.groupId == groupId)
            .ToList();

        foreach (var trigger in triggers)
        {
            trigger.DisablePermanently();
        }
    }
}
