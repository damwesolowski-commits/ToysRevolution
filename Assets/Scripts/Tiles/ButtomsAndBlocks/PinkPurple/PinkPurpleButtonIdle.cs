using UnityEngine;

public class PinkPurpleButtonIdle : PinkPurpleButtonBase
{
    protected override void Start()
    {
        base.Start();
        SetPressedVisuals(false);
        isLockedPressed = false;
    }
}
