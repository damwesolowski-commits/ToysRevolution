using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleport_OneTimeRegistry : TeleportRegistryBase
{
    public override List<TeleportBase> GetTeleportsByGroup(int groupId)
    {
        allTeleports = FindObjectsByType<TeleportBase>(FindObjectsSortMode.None)
                        .Where(t => t is Teleport_OneTime || t is Teleport_OneTime_Exit)
                        .ToList();

        return allTeleports.Where(t => t.groupId == groupId).ToList();
    }

    public override List<TeleportBase> GetAllTeleports()
    {
        return FindObjectsByType<TeleportBase>(FindObjectsSortMode.None)
               .Where(t => t is Teleport_OneTime || t is Teleport_OneTime_Exit)
               .ToList();
    }
}
