using UnityEngine;

public class ShowHealthBarOnClick : MonoBehaviour
{
    private HealthBar healthBar;

    // statyczne pole po to, żeby logika kliku wykonała się tylko raz na klatkę
    private static int lastFrameClickProcessed = -1;

    private void Awake()
    {
        // szukamy paska zdrowia w dzieciach obiektu (również nieaktywnych)
        healthBar = GetComponentInChildren<HealthBar>(true);
    }

    private void Update()
    {
        // reagujemy na LPM
        if (Input.GetMouseButtonDown(0))
        {
            // jeśli inna instancja tego skryptu już obsłużyła ten klik w tej klatce – nic nie rób
            if (lastFrameClickProcessed == Time.frameCount)
                return;

            lastFrameClickProcessed = Time.frameCount;
            HandleClickGlobal();
        }
    }

    // pokazuje pasek tylko dla TEGO Enemy
    private void ShowSelf()
    {
        if (healthBar != null)
        {
            healthBar.Show();
        }
    }

    // statyczna logika: raz na klatkę dla całej sceny
    private static void HandleClickGlobal()
    {
        // 1) chowamy paski wszystkich Enemy
        HideAllEnemyHealthBars();

        // 2) sprawdzamy, w co kliknął gracz
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = worldPos;

        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);
        if (hit.collider == null) return;

        // szukamy Enemy, w którego kliknięto
        ShowHealthBarOnClick clicked = hit.collider.GetComponentInParent<ShowHealthBarOnClick>();
        if (clicked != null)
        {
            clicked.ShowSelf();
        }
    }

    // chowa paski TYLKO u Enemy
    private static void HideAllEnemyHealthBars()
    {
        // szukamy wszystkich obiektów z tym skryptem (czyli Enemy)
        ShowHealthBarOnClick[] all = FindObjectsOfType<ShowHealthBarOnClick>(true);

        foreach (var s in all)
        {
            if (s.healthBar == null) continue;

            // szukamy właściciela paska (obiekt z Health)
            Health ownerHealth = s.healthBar.GetComponentInParent<Health>();
            if (ownerHealth == null) continue;

            GameObject owner = ownerHealth.gameObject;

            // uznajemy za Enemy, jeśli ma tag lub layer "Enemy"
            bool isEnemy =
                owner.CompareTag("Enemy") ||
                owner.layer == LayerMask.NameToLayer("Enemy");

            if (isEnemy)
            {
                s.healthBar.Hide();
            }
        }
    }
}
