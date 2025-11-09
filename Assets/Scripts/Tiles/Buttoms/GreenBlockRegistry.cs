using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GreenBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        if (allBlocks.Count == 0)
            allBlocks.AddRange(FindObjectsByType<GreenBlock>(FindObjectsSortMode.None));

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}

