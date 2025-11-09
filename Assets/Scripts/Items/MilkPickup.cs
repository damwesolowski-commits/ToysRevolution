using UnityEngine;

public class MilkPickup : MonoBehaviour
{
    [Header("Ustawienia")]
    [SerializeField] private int healAmount = 100;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private float rotationSpeed = 90f;

    private bool isCollected = false;
    private Vector2Int myTile; // pozycja w siatce

    private void Start()
    {
        // Zapamiętaj pozycję kafelka, na którym stoi mleko
        Vector3 pos = transform.position;
        myTile = new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
    }

    private void Update()
    {
        // 🔄 Obracanie butelki
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

        // 🧩 Sprawdzaj, czy jakiś Player stoi na tym samym kafelku
        if (isCollected) return;

        foreach (var mover in FindObjectsOfType<GridMover>())
        {
            // Sprawdzamy tylko Playera
            if (!mover.CompareTag("Player")) continue;

            if (mover.GetCurrentTile() == myTile)
            {
                TryCollect(mover.gameObject);
                break;
            }
        }
    }

    private void TryCollect(GameObject player)
    {
        if (isCollected) return;
        isCollected = true;

        var health = player.GetComponent<Health>();
        if (health != null)
        {
            health.Heal(healAmount);
        }

        // 🔊 Odtwórz dźwięk
        if (healSound != null)
            AudioSource.PlayClipAtPoint(healSound, transform.position);

        // 🧼 Zniszcz mleko po chwili
        Destroy(gameObject, 0.05f);
    }
}
