using UnityEngine;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class EndingButton : ButtonBase
{
    [Header("Ending Button Settings")]
    [Tooltip("Ile razy przycisk może zostać aktywowany")]
    public int maxClicks = 3;

    [Tooltip("Sprite'y Idle dla kolejnych liczników (np. [0]=0, [1]=1, [2]=2 ... [5]=5)")]
    public Sprite[] idleSprites;

    [Tooltip("Sprite'y Pressed dla kolejnych liczników (np. [0]=0, [1]=1, [2]=2 ... [5]=5)")]
    public Sprite[] pressedSprites;

    private int remainingClicks;
    private bool isLocked = false;
    private bool isPressed = false;

    protected override void Awake()
    {
        base.Awake();
        remainingClicks = maxClicks;

        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<BrownBlockRegistry>();

        UpdateSprites();
        SetPressedVisuals(false); // startowo idle
    }

    protected override void OnPressed(GameObject unit)
    {
        if (isLocked || blockRegistry == null) return;
        if (isPressed) return;

        isPressed = true;

        // Efekt wywołujemy na wejściu (klik)
        blockRegistry.ToggleGroup(groupId);

        // Pokaż wciśniętą wersję dla aktualnego licznika (np. Pressed 5)
        UpdateSprites();
        SetPressedVisuals(true);
    }

    protected override void OnReleased(GameObject unit)
    {
        if (!isPressed) return;

        // Schodzimy z przycisku -> teraz zużywamy 1 klik
        remainingClicks = Mathf.Max(remainingClicks - 1, 0);
        isPressed = false;

        if (remainingClicks <= 0)
        {
            isLocked = true;
            UpdateSprites();
            SetPressedVisuals(true);   // zostaje wciśnięty: Pressed 0
            return;
        }

        // Nadal można klikać -> pokaż Idle z nowym licz. (np. Idle 4)
        UpdateSprites();
        SetPressedVisuals(false);
    }

    // Aktualizuje parę: idleSprite / pressedSprite dla bieżącej wartości licznika.
    // NIE dotyka spriteRenderer.sprite – to robi SetPressedVisuals.
    private void UpdateSprites()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        int idxIdle = Mathf.Clamp(remainingClicks, 0, (idleSprites != null ? idleSprites.Length - 1 : 0));
        int idxPressed = Mathf.Clamp(remainingClicks, 0, (pressedSprites != null ? pressedSprites.Length - 1 : 0));

        if (idleSprites != null && idleSprites.Length > 0) idleSprite = idleSprites[idxIdle];
        if (pressedSprites != null && pressedSprites.Length > 0) pressedSprite = pressedSprites[idxPressed];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Podgląd w edytorze: pokaż Idle dla maxClicks
        int idxIdle = Mathf.Clamp(maxClicks, 0, (idleSprites != null ? idleSprites.Length - 1 : 0));
        int idxPressed = Mathf.Clamp(maxClicks, 0, (pressedSprites != null ? pressedSprites.Length - 1 : 0));

        if (idleSprites != null && idleSprites.Length > 0) idleSprite = idleSprites[idxIdle];
        if (pressedSprites != null && pressedSprites.Length > 0) pressedSprite = pressedSprites[idxPressed];

        spriteRenderer.sprite = idleSprite;
    }
}
#endif
