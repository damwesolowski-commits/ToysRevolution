using System.Collections;
using UnityEngine;

public class Teleport_MysteryEntrance : TeleportBase
{
    private Teleport_MysteryRegistry _registry;

    [Header("Mystery Entrance Settings")]
    [Tooltip("Czas, przez jaki wejście jest aktywne po aktywacji. " +
             "Jeśli <= 0, wejście nie znika z czasem i czeka, aż ktoś je wykorzysta.")]
    public float activationDuration = 10f;

    private bool isActive = false;
    private bool hasBeenUsed = false;

    // Czy to wejście powinno sterować globalną muzyką (zależne od Triggera)
    private bool playMusicForThisEntrance = false;
    private bool isRegisteredForMusic = false;

    private Coroutine lifetimeCoroutine;

    protected override TeleportRegistryBase Registry
    {
        get
        {
            if (_registry == null)
                _registry = FindFirstObjectByType<Teleport_MysteryRegistry>();
            return _registry;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        // Na starcie w grze wejście jest niewidoczne
        if (Application.isPlaying && spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    // Można wejść TYLKO gdy aktywne i nieużyte
    protected override bool CanTeleportFromThis()
    {
        return isActive && !hasBeenUsed;
    }

    // Wejście jest jednorazowe – po użyciu zostanie zniszczone przez TeleportBase
    protected override bool IsOneTime() => true;

    // Wejście nie może być celem teleportu, tylko punktem startowym
    public override bool CanBeExit() => false;

    // 🔹 Wywoływane przez Registry (trigger)
    public void Activate(bool playMusic)
    {
        if (hasBeenUsed)
            return; // użyte wejście nigdy nie aktywuje się ponownie

        isActive = true;
        playMusicForThisEntrance = playMusic;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        // Muzyka – rejestrujemy się w globalnym kontrolerze
        if (playMusicForThisEntrance && MysteryPassageAudio.Instance != null && !isRegisteredForMusic)
        {
            MysteryPassageAudio.Instance.RegisterEntranceActivation();
            isRegisteredForMusic = true;
        }

        // 🔥 AUTOMATYCZNE TELEPORTOWANIE JEDNOSTEK JUŻ STOJĄCYCH NA POLU
        var hits = Physics2D.OverlapPointAll(GetExitCenter());
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var unit = hit.GetComponent<GridMover>();
            if (unit == null) continue;

            // Jednostka stoi na wejściu → teleport natychmiast
            HandleTeleport(unit.gameObject);

            // Tylko jedna jednostka może użyć wejścia, więc przerywamy
            break;
        }

        // Jeżeli ma limit czasu – odpalamy licznik
        if (activationDuration > 0f)
        {
            if (lifetimeCoroutine != null)
                StopCoroutine(lifetimeCoroutine);

            lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(activationDuration);

        // Jeśli do tego czasu nikt nie skorzystał → wejście znika
        if (!hasBeenUsed)
        {
            Deactivate(hideSprite: true);
        }
    }

    // Deaktywacja (koniec czasu lub przygotowanie do zniknięcia po użyciu)
    private void Deactivate(bool hideSprite)
    {
        if (!isActive && !isRegisteredForMusic)
            return;

        isActive = false;

        if (hideSprite && spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (isRegisteredForMusic && MysteryPassageAudio.Instance != null)
        {
            MysteryPassageAudio.Instance.RegisterEntranceDeactivation();
            isRegisteredForMusic = false;
        }
    }

    protected override IEnumerator TeleportRoutine(GameObject unit)
    {
        hasBeenUsed = true;

        // zatrzymaj licznik czasu, jeśli działał
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // To wejście już nie jest aktywne dla kolejnych jednostek,
        // ale zostawiamy sprite, żeby TeleportBase mógł zrobić swoją animację.
        Deactivate(hideSprite: false);

        // Informujemy Registry, że jakieś wejście zostało użyte → Trigger w tej grupie staje się permanentnie nieaktywny
        var reg = Registry as Teleport_MysteryRegistry;
        if (reg != null)
        {
            reg.NotifyEntranceUsed(groupId);
        }

        // Najpierw wykonujemy standardowy teleport (przeniesienie jednostki do MysteryExit)
        yield return base.TeleportRoutine(unit);

        // 🔥 PO TELEPORCIE: sprawdzamy, czy na nowym polu nie ma MysteryTriggera
        Vector2 checkPos = unit.transform.position;
        var hits = Physics2D.OverlapPointAll(checkPos);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var trigger = hit.GetComponent<Teleport_MysteryTrigger>();
            if (trigger == null) continue;

            // Wywołujemy logikę triggera.
            // Jeśli jest permanentnie wyłączony, jego CanTeleportFromThis() i tak to zablokuje.
            trigger.HandleTeleport(unit);

            // Jeśli miałoby być kilka triggerów na jednym polu,
            // możesz zostawić pętlę bez break, ale w Twoim designie zwykle wystarczy jeden.
            // break;
        }

        // TeleportBase dalej usunie ten obiekt, bo IsOneTime() == true
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(GetExitCenter(), Vector3.one * 0.5f);
        Gizmos.DrawIcon(GetExitCenter(), "sv_label_1.png", true);
    }
#endif
}
