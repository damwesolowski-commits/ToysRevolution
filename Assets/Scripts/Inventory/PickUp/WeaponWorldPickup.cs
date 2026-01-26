using UnityEngine;

public class WeaponWorldPickup : GridPickupBase
{
    [SerializeField] private WeaponConfig weapon;
    [SerializeField] private SpriteRenderer sr;

    protected override void Start()
    {
        base.Start();

        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && weapon != null && weapon.icon != null)
            sr.sprite = weapon.icon;
    }

    protected override bool OnTryPickup(GameObject unit)
    {
        var inv = unit.GetComponent<UnitInventory>();
        if (inv == null) return false;
        if (!inv.CanPickupItems) return false;
        if (weapon == null) return false;

        if (inv.TryPickupWeapon(weapon))
        {
            Destroy(gameObject);
            return true;
        }

        return false;
    }
}
