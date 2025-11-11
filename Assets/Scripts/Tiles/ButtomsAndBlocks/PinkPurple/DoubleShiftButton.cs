using UnityEngine;
using System.Linq;

public class DoubleShiftButton : ButtonBase
{
    [Header("Starting State")]
    public bool startPressed = false;

    private bool isLockedPressed = false;

    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<PinkPurpleBlockRegistry>();
    }

    private void Start()
    {
        if (startPressed)
        {
            SetPressedVisuals(true);
            isLockedPressed = true;
        }
        else
        {
            SetPressedVisuals(false);
            isLockedPressed = false;
        }
    }

    protected override void OnPressed(GameObject unit)
    {
        // 🔒 Jeśli przycisk jest już zablokowany, zignoruj kliknięcie
        if (isLockedPressed) return;
        if (blockRegistry == null) return;

        // 🔹 1. Przełącz stan klocków (różowe <-> fioletowe)
        blockRegistry.ToggleGroup(groupId);

        // 🔹 2. Zablokuj ten przycisk
        SetPressedVisuals(true);
        isLockedPressed = true;

        // 🔹 3. Znajdź wszystkie inne przyciski z tej samej grupy
        var allButtons = FindObjectsByType<DoubleShiftButton>(FindObjectsSortMode.None)
            .Where(b => b.groupId == groupId && b != this)
            .ToList();

        foreach (var btn in allButtons)
        {
            // 🔹 Odkliknij inne
            btn.SetPressedVisuals(false);
            btn.isLockedPressed = false;

            // 🔹 Jeśli gracz stoi na tym przycisku — wymuś jego natychmiastową reakcję
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float dist = Vector2.Distance(player.transform.position, btn.transform.position);
                if (dist < 0.1f) // gracz stoi na przycisku
                {
                    // 🔁 Ręcznie uruchom jego OnPressed (symulacja wejścia)
                    btn.OnPressed(player);
                }
            }
        }
    }

    public override void HandleUnitStepOff(GameObject unit)
    {
        if (unit == null) return;
        if (isLockedPressed) return; // 🔹 nie odklikujemy
        base.HandleUnitStepOff(unit);
    }
}
