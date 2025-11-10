using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BrownBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        if (allBlocks.Count == 0)
            allBlocks.AddRange(FindObjectsByType<BrownBlock>(FindObjectsSortMode.None));

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
