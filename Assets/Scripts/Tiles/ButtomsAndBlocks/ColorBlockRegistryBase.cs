using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class ColorBlockRegistryBase : MonoBehaviour
{
    public List<ColorBlock> allBlocks = new List<ColorBlock>();
    public abstract List<ColorBlock> GetBlocksByGroup(int id);

    // 🔹 Przełączanie grupy (nowa wersja)
    public virtual void ToggleGroup(int id)
    {
        var blocks = GetBlocksByGroup(id);
        if (blocks == null || blocks.Count == 0)
        {
            //Debug.LogWarning($"[{GetType().Name}] Nie znaleziono żadnych bloków o ID {id}");
            return;
        }

        foreach (var block in blocks)
        {
            bool newState = !block.IsExtended;
            block.SetState(newState, initialize: false);
            Debug.Log($"[{GetType().Name}] Zmieniono stan bloku {block.name} → {(newState ? "WYSUNIĘTY" : "SCHOWANY")}");
        }

        //Debug.Log($"[{GetType().Name}] Przełączono (toggle) grupę {id} ({blocks.Count} bloków)");
    }

    public bool IsGroupExtended(int id)
    {
        var blocks = GetBlocksByGroup(id);
        if (blocks == null || blocks.Count == 0)
            return false;

        // Grupa jest uznawana za wysuniętą, jeśli choć jeden blok jest wysunięty
        return blocks.Any(b => b.IsExtended);
    }
}
