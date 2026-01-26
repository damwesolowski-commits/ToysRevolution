using UnityEngine;

public class PinkPurpleButtonPressed : PinkPurpleButtonBase
{
    protected override void Start()
    {
        base.Start();
        SetPressedVisuals(true);
        isLockedPressed = true;
    }
}
