using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeBlockRegistry : ColorBlockRegistryBase
{
    public override List<ColorBlock> GetBlocksByGroup(int id)
    {
        // 🔹 Zawsze pobieraj aktualne obiekty TimedColorBlock ze sceny
        var currentBlocks = FindObjectsByType<TimedColorBlock>(FindObjectsSortMode.None)
                            .Where(b => b != null)
                            .Cast<ColorBlock>()
                            .ToList();

        // 🔹 Upewnij się, że allBlocks istnieje (dla kompatybilności z systemem bazowym)
        allBlocks = currentBlocks;

        // 🔹 Zwróć tylko te z odpowiednim Group ID
        return currentBlocks.Where(b => b.groupId == id).ToList();
    }
}
