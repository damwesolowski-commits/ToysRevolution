using UnityEngine;

public class YellowArrowUpDown : YellowArrowBase
{
    [Header("Sprites")]
    public Sprite upSprite;
    public Sprite downSprite;

    public enum StartDirection
    {
        Down,
        Up
    }

    [Header("Settings")]
    public StartDirection pointingAtStart = StartDirection.Up;

    private bool isPointingUp;

    protected override void Awake()
    {
        base.Awake();
        isPointingUp = (pointingAtStart == StartDirection.Up);
        ApplyDirectionVisuals();
        UpdateGridArrowDirection(); // 🧩 Ustal kierunek w GridData przy starcie
    }

    protected override bool OnToggleDirection()
    {
        bool previous = isPointingUp;
        isPointingUp = !isPointingUp;
        ApplyDirectionVisuals();

        if (previous != isPointingUp)
            UpdateGridArrowDirection(); // 🧭 Aktualizacja kierunku w GridData

        return previous != isPointingUp;
    }

    private void ApplyDirectionVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = isPointingUp ? upSprite : downSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 🔹 Aktualizuj sprite w edytorze przy zmianie "Pointing At Start"
        isPointingUp = (pointingAtStart == StartDirection.Up);
        ApplyDirectionVisuals();
    }
#endif
}
