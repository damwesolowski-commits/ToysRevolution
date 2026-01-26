using UnityEngine;

public class YellowButton_EnterAndStay : ButtonBase
{
    private YellowArrowRegistry yellowArrowRegistry;

    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            yellowArrowRegistry = FindObjectOfType<YellowArrowRegistry>();
    }

    protected override void OnPressed(GameObject unit)
    {
        if (yellowArrowRegistry == null)
            yellowArrowRegistry = FindObjectOfType<YellowArrowRegistry>();

        yellowArrowRegistry.ToggleGroup(groupId);
    }

    protected override void OnReleased(GameObject unit)
    {
        if (yellowArrowRegistry == null)
            yellowArrowRegistry = FindObjectOfType<YellowArrowRegistry>();

        yellowArrowRegistry.ToggleGroup(groupId);
    }
}
