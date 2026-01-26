using UnityEngine;

[CreateAssetMenu(menuName = "ToysRevolution/Inventory/Weapon Config", fileName = "Weapon_")]
public class WeaponConfig : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Visual (optional)")]
    public Sprite holderSpriteOverride;

    [Header("Combat")]
    public int damage = 1;
    public float range = 1.0f;
    public float cooldownSeconds = 1.0f;
    public WeaponType weaponType = WeaponType.Melee;

    [Header("Special abilities (optional)")]
    public WeaponAbility[] abilities;

    // ===== HELPER =====
    public bool HasAbility<T>() where T : WeaponAbility
    {
        if (abilities == null) return false;

        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] is T) return true;
        }

        return false;
    }
}

public enum WeaponType
{
    Melee,
    Ranged
}
