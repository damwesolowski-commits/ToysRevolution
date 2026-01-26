using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrayBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        // Pobierz TYLKO bloki, których klasa w nazwie zawiera "Gray"
        allBlocks = FindObjectsByType<ColorBlock>(FindObjectsSortMode.None)
                    .Where(b => b != null && b.GetType().Name.Contains("Gray"))
                    .ToList();

        return allBlocks.Where(b => b.groupId == id).ToList();
    }
}
