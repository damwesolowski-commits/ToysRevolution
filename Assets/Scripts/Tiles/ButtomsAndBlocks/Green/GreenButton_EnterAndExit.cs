using UnityEngine;

public class GreenButton_EnterAndExit : ButtonBase
{
    private GreenBlockRegistry greenBlockRegistry;

    private bool isToggled = false;

    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<GreenBlockRegistry>();
    }
}
