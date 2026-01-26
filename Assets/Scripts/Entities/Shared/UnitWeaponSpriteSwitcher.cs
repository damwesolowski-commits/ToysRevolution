using UnityEngine;

[RequireComponent(typeof(UnitInventory))]
public class UnitWeaponSpriteSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Default sprite (when no override)")]
    [SerializeField] private Sprite defaultSprite;

    private UnitInventory inv;

    private void Awake()
    {
        inv = GetComponent<UnitInventory>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (defaultSprite == null && targetRenderer != null)
            defaultSprite = targetRenderer.sprite;
    }

    private void OnEnable()
    {
        inv.OnWeaponChanged += HandleWeaponChanged;

        // odśwież na start (gdy broń jest już ustawiona w Inspectorze)
        HandleWeaponChanged(inv.EquippedWeapon);
    }

    private void OnDisable()
    {
        inv.OnWeaponChanged -= HandleWeaponChanged;
    }

    private void HandleWeaponChanged(WeaponConfig weapon)
    {
        if (targetRenderer == null) return;

        if (weapon != null && weapon.holderSpriteOverride != null)
            targetRenderer.sprite = weapon.holderSpriteOverride;
        else
            targetRenderer.sprite = defaultSprite;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            if (inv == null)
                inv = GetComponent<UnitInventory>();

            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SpriteRenderer>();

            if (defaultSprite == null && targetRenderer != null)
                defaultSprite = targetRenderer.sprite;

            HandleWeaponChanged(inv != null ? inv.EquippedWeapon : null);
        }
    }
#endif
}
