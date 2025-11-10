using UnityEngine;

public class BlueButton_EnterAndStay : ButtonBase
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
        if (blockRegistry == null) return;
        blockRegistry.ToggleGroup(groupId);
    }
}
