using UnityEngine;

public class BlueButton_EnterAndExit : ButtonBase
{
    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<BlueBlockRegistry>();
    }

    protected override void OnPressed(GameObject unit)
    {
        if (blockRegistry == null) return;
        blockRegistry.ToggleGroup(groupId);
    }

    protected override void OnReleased(GameObject unit)
    {
        // nic nie robimy — most zostaje w stanie po naciśnięciu
    }
}

