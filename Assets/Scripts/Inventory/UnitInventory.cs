using UnityEngine;
using System;

public class UnitInventory : MonoBehaviour
{
    [Header("Pickup rules")]
    [Tooltip("Player = true. Enemy = false (nie może podnosić w trakcie gry).")]
    public bool CanPickupItems = true;

    [Header("Slots (runtime)")]
    [SerializeField] private WeaponConfig equippedWeapon; // 1 slot na broń
    [SerializeField] private ItemConfig carriedItem;       // 1 slot na item
    [SerializeField] private int carriedItemCount = 0;     // dla stack (np. Remains)

    // --- Public read-only (UI/AI) ---
    public WeaponConfig EquippedWeapon => equippedWeapon;
    public ItemConfig CarriedItem => carriedItem;
    public int CarriedItemCount => carriedItemCount;
    public bool HasFloatEquipped => HasWeaponAbility<FloatAbility>();

    public event Action<WeaponConfig> OnWeaponChanged;

    // ========== WEAPON ==========
    public void EquipWeapon(WeaponConfig newWeapon)
    {
        if (newWeapon == null) return;

        // Zasada: nowa broń nadpisuje starą, stara znika (nie dropimy)
        equippedWeapon = newWeapon;
        // TODO: event do UI jeśli chcesz
        OnWeaponChanged?.Invoke(equippedWeapon);
    }

    // ========== ITEM ==========
    public bool HasItem() => carriedItem != null && carriedItemCount > 0;

    public bool CanAcceptItem(ItemConfig item)
    {
        if (item == null) return false;

        // Jeśli pusty slot → ok
        if (carriedItem == null || carriedItemCount == 0) return true;

        // Stackowanie: tylko jeśli to ten sam item i jest stackowalny
        if (carriedItem == item && item.stackable) return true;

        return false;
    }

    /// <summary>
    /// Próbuje dodać item do slotu.
    /// Jeśli slot pusty → wkłada.
    /// Jeśli to ten sam stackowalny → zwiększa count.
    /// Jeśli zajęty innym → zwraca false (swap robimy wyżej, w logice pickupa).
    /// </summary>
    public bool TryAddItem(ItemConfig item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (carriedItem == null || carriedItemCount == 0)
        {
            carriedItem = item;
            carriedItemCount = amount;
            return true;
        }

        if (carriedItem == item && item.stackable)
        {
            carriedItemCount += amount;
            return true;
        }

        return false;
    }

    public void ClearItem()
    {
        carriedItem = null;
        carriedItemCount = 0;
    }

    /// <summary>
    /// Usuwa określoną ilość (dla stacków). Jeśli spadnie do 0 → czyści slot.
    /// </summary>
    public bool TryConsumeItem(int amount = 1)
    {
        if (amount <= 0) return false;
        if (!HasItem()) return false;

        carriedItemCount -= amount;
        if (carriedItemCount <= 0)
        {
            ClearItem();
        }
        return true;
    }

    // ========== PICKUP GATE (ważne dla Enemy) ==========
    public bool TryPickupWeapon(WeaponConfig weapon)
    {
        if (!CanPickupItems) return false;
        if (weapon == null) return false;

        EquipWeapon(weapon);
        return true;
    }

    public bool TryPickupItem(ItemConfig item, int amount = 1)
    {
        if (!CanPickupItems) return false;
        return TryAddItem(item, amount);
    }
    public bool HasWeaponAbility<T>() where T : WeaponAbility
    {
        if (equippedWeapon == null) return false;
        if (equippedWeapon.abilities == null) return false;

        for (int i = 0; i < equippedWeapon.abilities.Length; i++)
        {
            if (equippedWeapon.abilities[i] is T)
                return true;
        }

        return false;
    }
}
