using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class CyclicDeathTile : MonoBehaviour
{
    [Header("Czas działania pola")]

    [Tooltip("Opóźnienie przed rozpoczęciem pierwszego cyklu (w sekundach)")]
    public float delayTime = 0f;

    [Tooltip("Całkowity czas jednego cyklu (Active + Inactive)")]
    [SerializeField, ReadOnlyInspector]
    private float cycleTime;

    [Tooltip("Czas, przez jaki pole jest aktywne (zabija jednostki)")]
    public float activeTime = 1.0f;

    [Tooltip("Czas, przez jaki pole jest nieaktywne (bezpieczne)")]
    public float inactiveTime = 3.0f;

    [Header("Efekty wizualne i dźwiękowe")]
    [Tooltip("Sprite w fazie aktywnej (np. gejzer w wybuchu)")]
    public Sprite activeSprite;

    [Tooltip("Sprite w fazie nieaktywnej (np. spokojny gejzer)")]
    public Sprite inactiveSprite;

    [Tooltip("Dźwięk ostrzegawczy odtwarzany 1 sekundę przed wybuchem")]
    public AudioClip warningSound;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool isActive = false;
    private Vector3Int cellPosition;

    void OnValidate()
    {
        cycleTime = activeTime + inactiveTime;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("❌ CyclicDeathTile: Brak SpriteRenderer!");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("⚠️ CyclicDeathTile: Brak AudioSource – dźwięk nie zostanie odtworzony.");
        }

        // Ustaw sprite początkowy
        SetActiveState(false);

        // Rozpocznij działanie po opóźnieniu
        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        if (delayTime > 0)
            yield return new WaitForSeconds(delayTime);

        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            // 🔸 Faza nieaktywna (bezpieczna)
            SetActiveState(false);

            // 1 sekundę przed końcem fazy nieaktywnej – dźwięk ostrzegawczy
            if (inactiveTime > 1f && audioSource != null && warningSound != null)
            {
                yield return new WaitForSeconds(inactiveTime - 1f);
                audioSource.PlayOneShot(warningSound);
                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(inactiveTime);
            }

            // 🔥 Faza aktywna (zabójcza)
            SetActiveState(true);
            yield return new WaitForSeconds(activeTime);
        }
    }

    private void SetActiveState(bool state)
    {
        isActive = state;
        spriteRenderer.sprite = state ? activeSprite : inactiveSprite;
    }

    void Update()
    {
        if (!Application.isPlaying || !isActive) return;

        // 🔍 Sprawdź, czy ktoś stoi na tym polu
        Collider2D hit = Physics2D.OverlapPoint(transform.position);
        if (hit != null)
        {
            Health hp = hit.GetComponent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(hp.MaxHP); // natychmiastowa śmierć
                Debug.Log($"{hit.name} zginął na CyclicDeathTile ({transform.position})");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isActive ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
    }
}