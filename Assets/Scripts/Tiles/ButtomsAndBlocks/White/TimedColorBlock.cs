using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedColorBlock : ColorBlock
{
    [System.Serializable]
    public class CycleStep
    {
        public enum ActionType { Hide, Show }
        public ActionType action;
        [Min(0f)] public float duration = 0f;
    }

    [Header("Timed Cycle Settings")]
    public List<CycleStep> cycleSteps = new List<CycleStep>();

    private Coroutine activeCycle;
    private bool isCycleRunning = false;
    private bool initialState;

    private void Start()
    {
        initialState = isExtendedAtStart;
    }

    public void StartCycle()
    {
        if (cycleSteps == null || cycleSteps.Count == 0)
            return;

        if (activeCycle != null)
            StopCoroutine(activeCycle);

        activeCycle = StartCoroutine(RunCycle());
    }

    public void StopCycleAndReset()
    {
        if (activeCycle != null)
            StopCoroutine(activeCycle);

        SetState(initialState, initialize: false);
        isCycleRunning = false;
    }

    private IEnumerator RunCycle()
    {
        isCycleRunning = true;

        foreach (var step in cycleSteps)
        {
            switch (step.action)
            {
                case CycleStep.ActionType.Hide:
                    SetState(false, initialize: false);
                    break;
                case CycleStep.ActionType.Show:
                    SetState(true, initialize: false);
                    break;
            }

            if (step.duration > 0f)
                yield return new WaitForSeconds(step.duration);
        }

        // Po zakończeniu cyklu przywróć stan początkowy
        SetState(initialState, initialize: false);
        isCycleRunning = false;
    }

    public float GetTotalCycleTime()
    {
        float total = 0f;
        foreach (var step in cycleSteps)
            total += step.duration;
        return total;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 🔹 Jeśli lista pusta – dodaj automatycznie pierwszy krok (Show, 0 s)
        if (cycleSteps.Count == 0)
        {
            cycleSteps.Add(new CycleStep
            {
                action = CycleStep.ActionType.Show,
                duration = 0f
            });
        }

        // 🔹 Automatycznie przypisz Show/Hide na przemian
        for (int i = 0; i < cycleSteps.Count; i++)
        {
            var step = cycleSteps[i];
            var expectedAction = (i % 2 == 0)
                ? CycleStep.ActionType.Show
                : CycleStep.ActionType.Hide;

            if (step.action != expectedAction && UnityEditor.EditorApplication.isPlaying == false)
            {
                step.action = expectedAction;
            }
        }

        // 🔹 Aktualizacja sprite'a w edytorze w zależności od flagi Is Extended At Start
        if (!Application.isPlaying)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = isExtendedAtStart ? extendedSprite : hiddenSprite;
            }
        }
    }
#endif
}
