using UnityEngine;

/// <summary>
/// Pasek odliczający czas cooldownu po ataku.
/// Na początku cooldownu pasek jest PUSTY, potem się wypełnia do pełna
/// i wtedy znika.
/// </summary>
public class CooldownBar : MonoBehaviour
{
    [Header("Referencje sprite'ów")]
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Header("Ustawienia pozycji i szerokości")]
    [SerializeField] private float offsetY = 1.0f;   // wysokość nad jednostką
    [SerializeField] private float fullWidth = 1f;   // szerokość pełnego paska

    private Transform target;    // jednostka (parent)
    private bool isVisible = false;

    // logika cooldownu
    private bool isRunning = false;
    private float duration = 0f;
    private float elapsed = 0f;

    private void Start()
    {
        target = transform.parent;

        var parentRenderer = target.GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            fillRenderer.sortingLayerID = parentRenderer.sortingLayerID;
            backgroundRenderer.sortingLayerID = parentRenderer.sortingLayerID;

            fillRenderer.sortingOrder = parentRenderer.sortingOrder + 4;
            backgroundRenderer.sortingOrder = parentRenderer.sortingOrder + 3;
        }

        var c = new Color(0.2f, 0.7f, 1f, 1f);
        fillRenderer.color = c;

        HideImmediate();
    }

    private void LateUpdate()
    {
        // pozycja nad jednostką
        if (target != null)
            transform.position = target.position + new Vector3(0, offsetY, 0);

        if (!isRunning)
            return;

        elapsed += Time.deltaTime;
        float ratio = Mathf.Clamp01(elapsed / duration); // 0 → 1

        UpdateBarVisual(ratio);

        // cooldown skończony → pasek pełny, potem znika
        if (ratio >= 1f)
        {
            isRunning = false;
            HideImmediate();
        }
    }

    /// <summary>
    /// Wywołaj przy rozpoczęciu cooldownu po ataku.
    /// </summary>
    public void StartCooldown(float cooldownDuration)
    {
        if (cooldownDuration <= 0f)
            return;

        duration = cooldownDuration;
        elapsed = 0f;
        isRunning = true;

        // na starcie pusty pasek
        UpdateBarVisual(0f);
        Show();
    }

    /// <summary>
    /// Opcjonalnie – natychmiast przerwij i ukryj pasek.
    /// </summary>
    public void CancelCooldown()
    {
        isRunning = false;
        HideImmediate();
    }

    private void UpdateBarVisual(float ratio)
    {
        // szerokość paska od 0 do fullWidth
        fillRenderer.size = new Vector2(ratio * fullWidth, fillRenderer.size.y);

        // tło zawsze pełne
        backgroundRenderer.size = new Vector2(fullWidth, backgroundRenderer.size.y);

        // skracanie tylko z prawej strony (tak jak w HealthBarze)
        float offset = (1f - ratio) * fullWidth / 2f;
        fillRenderer.transform.localPosition = new Vector3(-offset, 0f, 0f);
        backgroundRenderer.transform.localPosition = Vector3.zero;
    }

    private void Show()
    {
        fillRenderer.enabled = true;
        backgroundRenderer.enabled = true;
        isVisible = true;
    }

    private void HideImmediate()
    {
        fillRenderer.enabled = false;
        backgroundRenderer.enabled = false;
        isVisible = false;
    }
    public bool IsCooldownActive()
    {
        return isRunning;
    }

    public float CooldownRemaining01()
    {
        if (!isRunning || duration <= 0f)
            return 0f;

        return Mathf.Clamp01(elapsed / duration);
    }
}
