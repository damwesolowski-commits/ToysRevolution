using UnityEngine;

public class PinkPurpleBlockExtended : PinkPurpleBlockBase
{
    private void Start()
    {
        // Ustawiamy stan startowy BEZ dźwięku (initialize: true)
        SetState(true, initialize: true);
    }
}

