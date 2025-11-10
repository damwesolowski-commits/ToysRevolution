using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class MultiPlayerButton : ButtonBase
{
    [Header("Multi-Player Settings")]
    [Range(2, 6)] public int requiredPlayers = 2; // liczba jednostek potrzebna do aktywacji
    [Tooltip("Sprite'y Idle dla 2–6 graczy")]
    public Sprite[] multiIdleSprites; // Sprite'y Idle (np. z 2–6 sylwetkami)
    [Tooltip("Sprite'y Pressed dla 2–6 graczy")]
    public Sprite[] multiPressedSprites; // Sprite'y Pressed (np. brązowe wersje z 2–6 sylwetkami)

    private bool isPressed = false;

    protected override void Awake()
    {
        base.Awake();
        ApplySprites();
        StartCoroutine(RegisterDelayed());
    }

    private IEnumerator RegisterDelayed()
    {
        yield return null; // poczekaj 1 klatkę aż menedżer się zainicjalizuje
        MultiPlayerButtonGroupManager.RegisterButton(this);
        Debug.Log($"[MultiPlayerButton] ✅ Zarejestrowano przycisk GroupID={groupId}, requiredPlayers={requiredPlayers}");
    }

    protected override void OnPressed(GameObject unit)
    {
        if (!isPressed)
        {
            isPressed = true;
            MultiPlayerButtonGroupManager.NotifyButtonPressed(groupId, this);
        }
    }

    protected override void OnReleased(GameObject unit)
    {
        if (isPressed)
        {
            isPressed = false;
            MultiPlayerButtonGroupManager.NotifyButtonReleased(groupId, this);
        }
    }

    // 🔹 Ustawia odpowiednie sprite’y Idle i Pressed zależnie od liczby wymaganych graczy
    private void ApplySprites()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        int index = Mathf.Clamp(requiredPlayers - 2, 0, 4); // indeks 0–4 dla 2–6 graczy

        if (multiIdleSprites != null && multiIdleSprites.Length > index)
            idleSprite = multiIdleSprites[index];

        if (multiPressedSprites != null && multiPressedSprites.Length > index)
            pressedSprite = multiPressedSprites[index];

        // od razu pokaż Idle Sprite w scenie
        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

#if UNITY_EDITOR
    // 🔹 Działa w edytorze — automatyczna zmiana sprite’a przy edycji wartości
    private void OnValidate()
    {
        ApplySprites();
    }
#endif
}
