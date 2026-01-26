using UnityEngine;

public class YellowButton_EnterAndExit : ButtonBase
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
}
