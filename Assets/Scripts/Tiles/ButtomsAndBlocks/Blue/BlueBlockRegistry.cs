using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlueBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        allBlocks = FindObjectsByType<BlueBlock>(FindObjectsSortMode.None)
                    .Cast<ColorBlock>()
                    .ToList();

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
