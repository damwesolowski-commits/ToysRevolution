using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GreenBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        // 🔹 Odświeża listę tylko z obiektów typu GreenBlock
        allBlocks = FindObjectsByType<GreenBlock>(FindObjectsSortMode.None)
                    .Cast<ColorBlock>()
                    .ToList();

        // 🔹 Zwraca tylko bloki o danym Group ID
        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
