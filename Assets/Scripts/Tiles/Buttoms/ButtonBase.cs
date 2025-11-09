using UnityEngine;

public abstract class ButtonBase : MonoBehaviour
{

    [Header("Group Settings")]
    public int groupId; 
    
    [Header("Button Visuals")]
    public Sprite idleSprite;
    public Sprite pressedSprite;

    [Header("Button Sounds")]
    public AudioClip pressSound;
    public AudioClip releaseSound;

    protected ColorBlockRegistryBase blockRegistry;
    protected SpriteRenderer spriteRenderer;
    protected AudioSource audioSource;

    // 🧍 Lista jednostek aktualnie stojących na przycisku
    private readonly System.Collections.Generic.List<GameObject> occupants = new();

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    // =====================================================
    // 🔹 Wywoływane przez TileLogicManager / GridMover
    // =====================================================

    public void HandleUnitStepOn(GameObject unit)
    {
        if (unit == null) return;
        if (!occupants.Contains(unit))
        {
            occupants.Add(unit);
            if (occupants.Count == 1)
            {
                SetPressedVisuals(true);
                PlaySound(pressSound);
                OnPressed(unit);
            }
        }
    }

    public void HandleUnitStepOff(GameObject unit)
    {
        if (unit == null) return;
        if (occupants.Contains(unit))
            occupants.Remove(unit);

        if (occupants.Count == 0)
        {
            SetPressedVisuals(false);
            PlaySound(releaseSound);
            OnReleased(unit);
        }
    }

    // =====================================================
    // 🔹 Wizualne i dźwiękowe efekty
    // =====================================================

    protected void SetPressedVisuals(bool pressed)
    {
        if (spriteRenderer == null) return;

        if (pressed && pressedSprite != null)
            spriteRenderer.sprite = pressedSprite;
        else if (!pressed && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        audioSource.PlayOneShot(clip);
    }

    // =====================================================
    // 🔹 Abstrakcyjne metody efektu przycisku
    // =====================================================

    // 🔹 Domyślna logika przycisku (można ją nadpisać w klasie pochodnej)
    protected virtual void OnPressed(GameObject unit)
    {
        if (blockRegistry == null)
        {
            Debug.LogWarning($"[{name}] Brak przypisanego rejestru bloków!");
            return;
        }

        bool groupExtended = blockRegistry.IsGroupExtended(groupId);
        blockRegistry.ToggleGroup(groupId);
        Debug.Log($"[{name}] Zmieniono stan grupy {groupId} na {(!groupExtended ? "wysunięty" : "schowany")}");
    }

    protected virtual void OnReleased(GameObject unit)
    {
        Debug.Log($"[{name}] Zwolniono przycisk grupy {groupId}");
    }
    private void Update()
    {
        // 🔹 Usuń z listy jednostki, które już nie istnieją (np. zostały zniszczone)
        for (int i = occupants.Count - 1; i >= 0; i--)
        {
            if (occupants[i] == null)
            {
                occupants.RemoveAt(i);
            }
        }

        // 🔹 Jeśli przycisk był wciśnięty, a lista jest już pusta — odkliknij
        if (occupants.Count == 0 && spriteRenderer != null && spriteRenderer.sprite == pressedSprite)
        {
            SetPressedVisuals(false);
            PlaySound(releaseSound);
            OnReleased(gameObject);
        }
    }
}
