using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BrownBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        allBlocks = FindObjectsByType<BrownBlock>(FindObjectsSortMode.None)
                    .Cast<ColorBlock>()
                    .ToList();

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
