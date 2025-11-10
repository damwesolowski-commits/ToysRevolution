using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlueBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        if (allBlocks.Count == 0)
            allBlocks.AddRange(FindObjectsByType<BlueBlock>(FindObjectsSortMode.None));

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
