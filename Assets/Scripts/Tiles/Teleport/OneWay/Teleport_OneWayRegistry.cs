using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleport_OneWayRegistry : TeleportRegistryBase
{
    public override List<TeleportBase> GetTeleportsByGroup(int groupId)
    {
        // Pobieramy ENTRY
        var entries = FindObjectsByType<Teleport_OneWay>(FindObjectsSortMode.None)
                        .Cast<TeleportBase>();

        // Pobieramy EXIT
        var exits = FindObjectsByType<Teleport_OneWay_Exit>(FindObjectsSortMode.None)
                        .Cast<TeleportBase>();

        // Łączymy listy
        allTeleports = entries.Concat(exits).ToList();

        return allTeleports.Where(t => t.groupId == groupId).ToList();
    }

    public override List<TeleportBase> GetAllTeleports()
    {
        // ENTRY
        var entries = FindObjectsByType<Teleport_OneWay>(FindObjectsSortMode.None)
                        .Cast<TeleportBase>();

        // EXIT
        var exits = FindObjectsByType<Teleport_OneWay_Exit>(FindObjectsSortMode.None)
                        .Cast<TeleportBase>();

        // Łączymy listy
        return entries.Concat(exits).ToList();
    }
}
