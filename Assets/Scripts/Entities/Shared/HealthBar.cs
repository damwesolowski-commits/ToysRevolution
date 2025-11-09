using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private float offsetY = 0.8f;

    private Health targetHealth;
    private Transform target;
    private bool isVisible;

    private void Start()
    {
        target = transform.parent; // jednostka
        targetHealth = target.GetComponent<Health>();

        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += UpdateHealthBar;
            targetHealth.OnDied += OnTargetDied;
            UpdateHealthBar(targetHealth.CurrentHP, targetHealth.MaxHP);
        }

        Hide(); // 🟡 Ukryj pasek na start
    }

    private void LateUpdate()
    {
        if (target != null)
            transform.position = target.position + new Vector3(0, offsetY, 0);
    }

    private void UpdateHealthBar(int currentHP, int maxHP)
    {
        float ratio = (float)currentHP / maxHP;
        float fullWidth = 1f; // szerokość paska przy 100% HP (dopasuj jeśli inna)
        fillRenderer.size = new Vector2(ratio * fullWidth, fillRenderer.size.y);

        // przesuń fill tak, żeby lewa krawędź była nieruchoma
        float offset = (1f - ratio) * fullWidth / 2f;
        fillRenderer.transform.localPosition = new Vector3(-offset, 0f, 0f);

        if (ratio > 0.7f) fillRenderer.color = Color.green;
        else if (ratio > 0.3f) fillRenderer.color = Color.yellow;
        else fillRenderer.color = Color.red;
    }

    public void Show()
    {
        fillRenderer.enabled = true;
        backgroundRenderer.enabled = true;
        isVisible = true;
    }

    public void Hide()
    {
        if (!Application.isPlaying) return;
        fillRenderer.enabled = false;
        backgroundRenderer.enabled = false;
        isVisible = false;
    }

    private void OnTargetDied()
    {
        Hide();
    }
}
