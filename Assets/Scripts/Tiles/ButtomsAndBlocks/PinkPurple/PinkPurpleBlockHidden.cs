using UnityEngine;

public class PinkPurpleBlockHidden : PinkPurpleBlockBase
{
    private void Start()
    {
        // Ustawiamy stan startowy BEZ dźwięku (initialize: true)
        SetState(false, initialize: true);
    }
}
