using UnityEngine;
using System.Linq;

public abstract class PinkPurpleButtonBase : ButtonBase
{
    protected bool isLockedPressed = false;

    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<PinkPurpleBlockRegistry>();
        suppressRelease = true; // przycisk nigdy nie odskakuje automatycznie
    }

    protected virtual void Start()
    {
        // konkretny stan początkowy ustawią klasy dziedziczące
    }

    protected override void OnPressed(GameObject unit)
    {
        // 🔒 Jeśli już wciśnięty, zignoruj
        if (isLockedPressed) return;
        if (blockRegistry == null) return;

        // 🔹 1. Przełącz blok grupy
        blockRegistry.ToggleGroup(groupId);

        // 🔹 2. Przełącz stan wszystkich przycisków PinkPurple z tej samej grupy
        var allButtons = FindObjectsByType<PinkPurpleButtonBase>(FindObjectsSortMode.None)
            .Where(b => b.groupId == groupId)
            .ToList();

        foreach (var btn in allButtons)
        {
            // jeśli był wciśnięty → odkliknij
            if (btn.isLockedPressed)
            {
                btn.SetPressedVisuals(false);
                btn.isLockedPressed = false;
            }
            // jeśli był odkliknięty → wciśnij
            else
            {
                btn.SetPressedVisuals(true);
                btn.isLockedPressed = true;
            }
        }
    }
}
