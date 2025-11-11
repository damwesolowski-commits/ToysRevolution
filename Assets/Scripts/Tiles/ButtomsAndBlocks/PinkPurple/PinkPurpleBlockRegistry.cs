using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PinkPurpleBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        if (allBlocks.Count == 0)
            allBlocks.AddRange(FindObjectsByType<PinkPurpleBlock>(FindObjectsSortMode.None));

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
