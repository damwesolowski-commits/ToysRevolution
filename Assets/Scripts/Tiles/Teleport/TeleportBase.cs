using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TeleportBase : MonoBehaviour
{
    [Header("Group Settings")]
    public int groupId;

    [Header("Teleport Visuals")]
    public Sprite idleSprite;
    public Sprite activeSprite;

    [Header("Teleport Sounds")]
    public AudioClip enterSound;
    public AudioClip exitSound;

    protected SpriteRenderer spriteRenderer;
    protected AudioSource audioSource;
    private readonly List<GameObject> occupants = new();

    // 🔹 Każdy konkretny teleport wskaże SWÓJ rejestr (BiDirectional / OneWay / OneTime)
    protected abstract TeleportRegistryBase Registry { get; }

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    // ───────────────────────────────
    // 🔹 Wywoływane przez TileLogicManager
    // ───────────────────────────────
    public void HandleTeleport(GameObject unit)
    {
        if (!CanTeleportFromThis()) return;

        StartCoroutine(TeleportRoutine(unit));
    }

    public void HandleUnitStepOn(GameObject unit)
    {
        // Jeśli jednostka już stoi na teleporcie → NIE uruchamiamy teleportu
        if (occupants.Contains(unit))
            return;

        occupants.Add(unit);

        // Dopiero teraz pozwalamy na teleport
        HandleTeleport(unit);
    }

    public void HandleUnitStepOff(GameObject unit)
    {
        if (occupants.Contains(unit))
            occupants.Remove(unit);
    }

    public void RegisterOccupantSilently(GameObject unit)
    {
        // Dodaj jednostkę do listy, ale NIE uruchamiaj teleportu
        if (unit == null) return;

        if (!occupants.Contains(unit))
            occupants.Add(unit);
    }

    /// <summary>
    /// Zabija wszystkie jednostki stojące na polu wyjścia (poza teleportowaną).
    /// </summary>
    protected void KillUnitsStandingOnExit(Vector3 exitPos, GameObject teleportingUnit)
    {
        var hits = Physics2D.OverlapPointAll(exitPos);

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            var other = hit.gameObject;

            if (other == teleportingUnit)
                continue;

            var hp = other.GetComponent<Health>();
            if (hp == null)
                continue;

            if (occupants.Contains(other))
                occupants.Remove(other);

            hp.TakeDamage(hp.MaxHP);
        }
    }

    // 🔹 Domyślnie KAŻDY teleport może być wyjściem
    public virtual bool CanBeExit()
    {
        return true;
    }

    protected virtual IEnumerator TeleportRoutine(GameObject unit)
    {
        // 1) Czy jednostka była zaznaczona? (do kamery)
        bool playerWasSelected = false;
        SelectableHighlight highlight = unit.GetComponent<SelectableHighlight>();
        if (unit.CompareTag("Player") && highlight != null && highlight.IsSelected)
            playerWasSelected = true;

        // 2) Zmień sprite na aktywny
        if (spriteRenderer != null && activeSprite != null)
            spriteRenderer.sprite = activeSprite;

        // 3) Dźwięk wejścia
        PlaySound(enterSound);

        // 4) Zniknij jednostkę
        GridMover mover = unit.GetComponent<GridMover>();
        if (mover != null) mover.enabled = false;

        // 🔹 Zapamiętaj stan wszystkich SpriteRendererów
        SpriteRenderer[] renders = unit.GetComponentsInChildren<SpriteRenderer>();
        bool[] wasEnabled = new bool[renders.Length];

        for (int i = 0; i < renders.Length; i++)
        {
            wasEnabled[i] = renders[i].enabled;
            renders[i].enabled = false;
        }

        // 5) 2 sekundy
        yield return new WaitForSeconds(2f);

        // 6) Pobierz rejestr odpowiedni dla TEGO typu teleportu
        var registry = Registry;
        if (registry == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Brak rejestru teleportów w scenie.");
            yield break;
        }

        // 7) Wybierz inny teleport z tej samej grupy (ale tylko tego samego typu)
        TeleportBase target = registry.GetRandomTargetTeleport(this);

        if (target == null)
        {
            Debug.LogWarning($"Teleport group {groupId} has no other teleports!");
            yield break;
        }

        Vector3 exitPos = target.GetExitCenter();

        // Najpierw zabij jednostki stojące na polu wyjściowym (Player/Enemy)
        target.KillUnitsStandingOnExit(exitPos, unit);

        // 🔁 Przenieś „zajęcie” z teleportu źródłowego na docelowy
        HandleUnitStepOff(unit);
        target.RegisterOccupantSilently(unit);

        // 8) Teleportuj
        mover.TeleportTo(exitPos);

        // 9) Kamera tylko jeśli Player był zaznaczony
        if (playerWasSelected && unit.CompareTag("Player"))
            FocusCamera(exitPos);

        // 10) Przywróć poprzedni stan rendererów
        for (int i = 0; i < renders.Length; i++)
        {
            if (renders[i] != null)
                renders[i].enabled = wasEnabled[i];
        }

        // 11) Włącz ruch
        mover.enabled = true;

        // 12) Dźwięk wyjścia
        PlaySound(exitSound);

        // 13) Jednorazowy teleport usuwa się po użyciu
        if (IsOneTime())
        {
            Destroy(gameObject);
        }
        else
        {
            // powrót do idle
            if (spriteRenderer != null && idleSprite != null)
                spriteRenderer.sprite = idleSprite;
        }
    }

    // ───────────────────────────────
    // 🔧 Pomocnicze
    // ───────────────────────────────

    protected void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.PlayOneShot(clip);
    }

    public Vector3 GetExitCenter()
    {
        return new Vector3(
            Mathf.Floor(transform.position.x) + 0.5f,
            Mathf.Floor(transform.position.y) + 0.5f,
            0f
        );
    }

    protected void FocusCamera(Vector3 newPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        camPos.x = newPos.x;
        camPos.y = newPos.y;
        cam.transform.position = camPos;
    }

    // ───────────────────────────────
    // 🔸 Abstrakcje do nadpisania
    // ───────────────────────────────
    protected abstract bool CanTeleportFromThis(); // np. OneTime_Exit blokuje wejście
    protected abstract bool IsOneTime();
}
