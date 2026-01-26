using UnityEngine;

public class SelectableHighlight : MonoBehaviour
{
    [Header("Ring Settings")]
    public GameObject selectionRingPrefab; // Prefab zielonego ringu
    private GameObject activeRingInstance;

    [Header("Tile Settings")]
    public float tileSize = 1f; // 1 jednostka = 1 tile, jeśli 64px przy 64 PPU to zostaw 1

    public bool IsSelected { get; private set; } = false;

    void Awake()
    {
        IsSelected = false;
    }

    public void SetSelected(bool value)
    {
        // Jeśli stan się nie zmienia — wyjdź (to eliminuje "toggle" przez przypadek)
        if (IsSelected == value)
            return;

        IsSelected = value;

        // 🔹 Odwołanie do HealthBar dziecka (jeśli istnieje)
        var healthBar = GetComponentInChildren<HealthBar>();

        if (IsSelected)
        {
            // Włącz ring
            if (selectionRingPrefab != null && activeRingInstance == null)
            {
                activeRingInstance = Instantiate(selectionRingPrefab, transform.position, Quaternion.identity);
                activeRingInstance.transform.SetParent(transform);
                activeRingInstance.transform.localPosition = Vector3.zero;

                // Dopasowanie skali ringu do rozmiaru kafelka
                float ringSpriteSize = GetSpriteWorldSize(activeRingInstance);
                if (ringSpriteSize > 0f)
                {
                    float scale = tileSize / ringSpriteSize;
                    activeRingInstance.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            // 🔹 Pokaż pasek HP
            if (healthBar != null)
                healthBar.OnSelected();
        }
        else
        {
            // Wyłącz ring
            if (activeRingInstance != null)
            {
                Destroy(activeRingInstance);
                activeRingInstance = null;
            }

            // 🔹 Ukryj pasek HP
            if (healthBar != null)
                healthBar.OnDeselected();
        }
    }

    private float GetSpriteWorldSize(GameObject obj)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return 0;
        return sr.sprite.bounds.size.x; // szerokość w jednostkach świata
    }
}
