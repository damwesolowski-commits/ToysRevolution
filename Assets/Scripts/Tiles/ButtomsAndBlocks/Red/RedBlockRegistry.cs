using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RedBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        allBlocks = FindObjectsByType<RedBlock>(FindObjectsSortMode.None)
                    .Cast<ColorBlock>()
                    .ToList();

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}

