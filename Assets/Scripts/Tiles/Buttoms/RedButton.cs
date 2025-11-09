using UnityEngine;

public class RedButton : ButtonBase
{
    private RedBlockRegistry redBlockRegistry;

    private bool isToggled = false;

    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<RedBlockRegistry>();
    }
}
