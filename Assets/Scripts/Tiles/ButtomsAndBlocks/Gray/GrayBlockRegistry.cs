using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrayBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        // zwróć WSZYSTKIE ColorBlocki z tym groupId – na start bez dalszego filtrowania
        return allBlocks.Where(b => b != null && b.groupId == id).ToList();
    }

    private void Awake()
    {
        // zbierz wszystkie ColorBlocki istniejące w scenie
        allBlocks = FindObjectsOfType<ColorBlock>(true).ToList();
    }
}
