using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PinkPurpleBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        allBlocks = FindObjectsByType<PinkPurpleBlockBase>(FindObjectsSortMode.None)
                    .Cast<ColorBlock>()
                    .ToList();

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
