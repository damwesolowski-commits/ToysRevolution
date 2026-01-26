using UnityEngine;
using System.Collections;

public class HealthBar : MonoBehaviour
{
    [Header("Referencje sprite'ów")]
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Header("Warunki pokazywania paska")]
    [SerializeField] private float damageShowDuration = 7f;   // ile sekund po obrażeniach
    [SerializeField] private float fadeOutDuration = 0.5f;    // czas miękkiego zanikania

    [Header("Ustawienia pozycji i szerokości")]
    [SerializeField] private float offsetY = 1f;
    [SerializeField] private float fullWidth = 1f; // szerokość całego paska

    private Coroutine autoHideCoroutine;
    private Coroutine fadeCoroutine;

    private Health targetHealth;
    private Transform target;
    private bool isVisible;

    // flaga zaznaczenia jednostki
    private bool isSelected = false;

    private void Start()
    {
        // parent, czyli jednostka (Player / Enemy)
        target = transform.parent;

        // 1) Podłącz się do komponentu Health na tej jednostce
        if (target != null)
        {
            targetHealth = target.GetComponent<Health>();
            if (targetHealth != null)
            {
                // subskrypcja eventów
                targetHealth.OnHealthChanged += UpdateHealthBar;
                targetHealth.OnDied += OnTargetDied;
                targetHealth.OnDamaged += HandleDamaged;

                // od razu ustaw poprawny wygląd paska na aktualne HP
                UpdateHealthBar(targetHealth.CurrentHP, targetHealth.MaxHP);
            }
        }

        // 2) Ustawianie sorting layer / order jak wcześniej
        var parentRenderer = target.GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            fillRenderer.sortingLayerID = parentRenderer.sortingLayerID;
            backgroundRenderer.sortingLayerID = parentRenderer.sortingLayerID;

            fillRenderer.sortingOrder = parentRenderer.sortingOrder + 4;
            backgroundRenderer.sortingOrder = parentRenderer.sortingOrder + 3;
        }

        // 3) Na starcie pasek ma być schowany
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateHealthBar;
            targetHealth.OnDied -= OnTargetDied;
            targetHealth.OnDamaged -= HandleDamaged;
        }
    }

    private void LateUpdate()
    {
        if (target != null)
            transform.position = target.position + new Vector3(0, offsetY, 0);
    }

    private void UpdateHealthBar(int currentHP, int maxHP)
    {
        float ratio = (float)currentHP / maxHP;
        // zielony pasek – proporcjonalnie do HP
        fillRenderer.size = new Vector2(ratio * fullWidth, fillRenderer.size.y);

        // tło – zawsze pełna szerokość
        backgroundRenderer.size = new Vector2(fullWidth, backgroundRenderer.size.y);

        // przesunięcie zielonego tak, żeby skracał się tylko z prawej
        float offset = (1f - ratio) * fullWidth / 2f;
        fillRenderer.transform.localPosition = new Vector3(-offset, 0f, 0f);

        // tło wycentrowane
        backgroundRenderer.transform.localPosition = Vector3.zero;

        // kolor, zawsze z pełną alfą (fade steruje alfą później)
        Color c;
        if (ratio > 0.7f) c = Color.green;
        else if (ratio > 0.3f) c = Color.yellow;
        else c = Color.red;

        c.a = 1f;
        fillRenderer.color = c;
    }

    public void Show()
    {
        // zatrzymaj ewentualny fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // przywróć pełną alfę (nie ruszamy koloru HP)
        var fillColor = fillRenderer.color;
        fillColor.a = 1f;
        fillRenderer.color = fillColor;

        var bgColor = backgroundRenderer.color;
        bgColor.a = 1f;
        backgroundRenderer.color = bgColor;

        fillRenderer.enabled = true;
        backgroundRenderer.enabled = true;
        isVisible = true;
    }

    public void Hide()
    {
        if (!Application.isPlaying) return;

        // ⭐ ZMIANA: jeżeli jednostka jest zaznaczona, NIGDY nie chowamy paska
        if (isSelected)
            return;

        fillRenderer.enabled = false;
        backgroundRenderer.enabled = false;
        isVisible = false;
    }

    // 🔹 Wywoływane z Health, gdy jednostka dostaje obrażenia
    private void HandleDamaged()
    {
        Show();  // natychmiast pokaż pasek

        // jeśli jednostka jest zaznaczona – nie ustawiamy licznika
        if (isSelected)
            return;

        // restart licznika auto-hide
        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);

        autoHideCoroutine = StartCoroutine(HideAfterDelay(damageShowDuration));
    }

    // 🔹 licznik 7 sekund, po którym zaczyna się fade
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        autoHideCoroutine = null;

        // jeśli w międzyczasie jednostka została zaznaczona – nie chowamy
        if (isSelected)
            yield break;

        // uruchom miękkie zanikanie
        fadeCoroutine = StartCoroutine(FadeOutAndHide());
    }

    // 🔹 miękkie zanikanie (fade out) paska
    private IEnumerator FadeOutAndHide()
    {
        float elapsed = 0f;

        Color fillStart = fillRenderer.color;
        Color bgStart = backgroundRenderer.color;

        // upewnij się że renderery są włączone
        fillRenderer.enabled = true;
        backgroundRenderer.enabled = true;

        while (elapsed < fadeOutDuration)
        {
            // ⭐ ZMIANA: jeśli w trakcie zanikania jednostka zostanie zaznaczona – PRZERYWAMY fade
            if (isSelected)
            {
                Show();            // przywróć pełny, widoczny pasek
                fadeCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);

            fillRenderer.color = new Color(fillStart.r, fillStart.g, fillStart.b, alpha);
            backgroundRenderer.color = new Color(bgStart.r, bgStart.g, bgStart.b, alpha);

            yield return null;
        }

        Hide();
        fadeCoroutine = null;
    }

    // 🔹 wywoływane, gdy jednostka umiera
    private void OnTargetDied()
    {
        // przy śmierci i tak chcemy ukryć pasek
        isSelected = false;   // ⭐ żeby Hide mógł zadziałać
        Hide();
    }

    // ============================
    // PUBLICZNE METODY SELEKCJI
    // ============================

    // Wywołaj to, gdy jednostka zostaje zaznaczona (np. po kliknięciu)
    public void OnSelected()
    {
        isSelected = true;

        Show(); // pasek ma być zawsze widoczny, gdy jednostka jest zaznaczona

        // żadnych auto-hide'ów podczas zaznaczenia
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    // Wywołaj to, gdy jednostka zostaje odznaczona
    public void OnDeselected()
    {
        isSelected = false;

        // jeśli jednostka ma pełne HP – schowaj od razu
        if (targetHealth != null && targetHealth.CurrentHP >= targetHealth.MaxHP)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            Hide();
            return;
        }

        // jeśli jednostka jest zraniona – uruchom licznik 7 s
        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);

        autoHideCoroutine = StartCoroutine(HideAfterDelay(damageShowDuration));
    }
    // Natychmiastowe ukrycie paska (bez żadnych animacji)
    private void HideImmediate()
    {
        fillRenderer.enabled = false;
        backgroundRenderer.enabled = false;
        isVisible = false;
    }
}
