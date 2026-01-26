using UnityEngine;

public abstract class PinkPurpleBlockBase : ColorBlock
{
    // Każda klasa pochodna ustawi stan początkowy (extended / hidden) w Start()

#if UNITY_EDITOR
    // Edytorowy podgląd w Scene View bez Play
    private void OnValidate()
    {
        // Używamy lokalnego GetComponent, bo spriteRenderer w ColorBlock jest prywatny
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (isExtendedAtStart && extendedSprite != null)
            sr.sprite = extendedSprite;
        else if (!isExtendedAtStart && hiddenSprite != null)
            sr.sprite = hiddenSprite;
    }
#endif
}
