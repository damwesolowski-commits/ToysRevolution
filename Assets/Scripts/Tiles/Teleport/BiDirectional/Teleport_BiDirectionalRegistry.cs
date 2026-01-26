using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teleport_BiDirectionalRegistry : TeleportRegistryBase
{
    public override List<TeleportBase> GetTeleportsByGroup(int groupId)
    {
        allTeleports = FindObjectsByType<Teleport_BiDirectional>(FindObjectsSortMode.None)
                       .Cast<TeleportBase>()
                       .ToList();

        return allTeleports.Where(t => t.groupId == groupId).ToList();
    }

    public override List<TeleportBase> GetAllTeleports()
    {
        return FindObjectsByType<Teleport_BiDirectional>(FindObjectsSortMode.None)
               .Cast<TeleportBase>()
               .ToList();
    }
}
