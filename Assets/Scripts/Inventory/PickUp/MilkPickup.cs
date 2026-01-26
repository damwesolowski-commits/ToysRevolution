using UnityEngine;

public class MilkPickup : GridPickupBase
{
    [Header("Ustawienia")]
    [SerializeField] private int healAmount = 100;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private float rotationSpeed = 90f;

    private void Update()
    {
        // 🔄 Obracanie butelki (czysto wizualne)
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }

    protected override bool OnTryPickup(GameObject unit)
    {
        var inv = unit.GetComponent<UnitInventory>();
        if (inv == null) return false;
        if (!inv.CanPickupItems) return false;

        var health = unit.GetComponent<Health>();
        if (health == null) return false;

        // ❤️ Leczenie
        health.Heal(healAmount);

        // 🔊 Dźwięk
        if (healSound != null)
            AudioSource.PlayClipAtPoint(healSound, transform.position);

        // 🧼 Usuń mleko
        Destroy(gameObject);
        return true;
    }
}
