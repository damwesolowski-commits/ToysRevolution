using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class TimeButton : ButtonBase
{
    [Header("Timer Settings")]
    public TextMeshPro textMesh; // obiekt z licznikiem sekund

    private Coroutine timerCoroutine;
    private float remainingTime;

    // Czas krótkiej fazy resetu, podczas której wszystkie klocki są w stanie początkowym
    private const float RESET_PHASE = 0.5f;

    protected override void Awake()
    {
        base.Awake();
        if (blockRegistry == null)
            blockRegistry = FindObjectOfType<TimeBlockRegistry>();
    }

    protected override void OnPressed(GameObject unit)
    {
        if (blockRegistry == null) return;

        // Jeśli cykl trwa — przerwij i natychmiast przywróć bloki do stanu początkowego
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            ResetAllBlocks();
        }

        // Zbierz wszystkie bloki czasowe w tej grupie
        var blocks = blockRegistry.GetBlocksByGroup(groupId)
            .OfType<TimedColorBlock>()
            .ToList();

        if (blocks.Count == 0)
            return;

        // Oblicz długość najdłuższego cyklu
        float maxCycle = 0f;
        foreach (var block in blocks)
        {
            float total = block.GetTotalCycleTime();
            if (total > maxCycle) maxCycle = total;
        }

        // Całkowity czas = krótka faza resetu + najdłuższy cykl
        float totalWithReset = RESET_PHASE + maxCycle;

        remainingTime = Mathf.Ceil(totalWithReset);
        timerCoroutine = StartCoroutine(TimerCountdownWithReset(blocks, maxCycle, RESET_PHASE));
    }

    private IEnumerator TimerCountdownWithReset(List<TimedColorBlock> blocks, float cycleTime, float resetPhase)
    {
        // 1) Utrzymaj stan początkowy przez resetPhase
        // (na wypadek, gdyby OnPressed nie był wywołany z aktywnym timerem)
        foreach (var block in blocks)
            block.StopCycleAndReset();

        float elapsed = 0f;
        float totalTime = resetPhase + cycleTime;

        // Aktualizuj licznik podczas krótkiej fazy resetu
        while (elapsed < resetPhase)
        {
            remainingTime = Mathf.Ceil(totalTime - elapsed);
            UpdateTimerText();
            // resetPhase = 0.5 s -> wystarczy pojedynczy wait, żeby nie „migać” odliczaniem
            yield return new WaitForSeconds(resetPhase - elapsed);
            elapsed = resetPhase;
        }

        // 2) Start właściwych cykli
        foreach (var block in blocks)
            block.StartCycle();

        // 3) Odliczanie czasu trwania cykli
        float cycleElapsed = 0f;
        while (cycleElapsed < cycleTime)
        {
            remainingTime = Mathf.Ceil(totalTime - (resetPhase + cycleElapsed));
            UpdateTimerText();
            yield return new WaitForSeconds(1f);
            cycleElapsed += 1f;
        }

        // 4) Koniec — posprzątaj licznik (bloki same wrócą do stanu początkowego po swoim cyklu)
        remainingTime = 0f;
        UpdateTimerText();
        timerCoroutine = null;
    }

    private void ResetAllBlocks()
    {
        var blocks = blockRegistry.GetBlocksByGroup(groupId)
            .OfType<TimedColorBlock>()
            .ToList();

        foreach (var block in blocks)
            block.StopCycleAndReset();
    }
    private void UpdateTimerText()
    {
        if (textMesh == null)
        {
            Debug.LogWarning($"[{name}] ❌ Brak przypisanego obiektu TextMeshPro do TimeButton!");
            return;
        }

        string displayText = remainingTime > 0 ? remainingTime.ToString("0") : "";

        textMesh.text = displayText;

        // 🔍 Diagnoza: pokaż aktualny tekst w konsoli (zobaczymy, czy licznik działa logicznie)
        Debug.Log($"[{name}] Timer update → {displayText}");
    }
}
