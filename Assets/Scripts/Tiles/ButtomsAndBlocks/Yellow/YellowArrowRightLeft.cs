using UnityEngine;

public class YellowArrowRightLeft : YellowArrowBase
{
    [Header("Sprites")]
    public Sprite rightSprite;
    public Sprite leftSprite;

    public enum StartDirection
    {
        Left,
        Right
    }

    [Header("Settings")]
    public StartDirection pointingAtStart = StartDirection.Right;

    private bool isPointingRight;

    protected override void Awake()
    {
        base.Awake();
        isPointingRight = (pointingAtStart == StartDirection.Right);
        ApplyDirectionVisuals();
        UpdateGridArrowDirection(); // 🧩 Ustal kierunek w GridData przy starcie
    }

    protected override bool OnToggleDirection()
    {
        bool previous = isPointingRight;
        isPointingRight = !isPointingRight;
        ApplyDirectionVisuals();

        if (previous != isPointingRight)
            UpdateGridArrowDirection(); // 🧭 Aktualizacja kierunku w GridData

        return previous != isPointingRight;
    }

    private void ApplyDirectionVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = isPointingRight ? rightSprite : leftSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 🔹 Aktualizuj kierunek w edytorze
        isPointingRight = (pointingAtStart == StartDirection.Right);
        ApplyDirectionVisuals();
    }
#endif
}
