using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationVariantRandomizer : MonoBehaviour
{
    public enum PickMode
    {
        OnEnter,        // wylosuj raz, gdy wejdziemy w stan
        CycleOnEnd      // losuj w kółko: po zakończeniu klipu -> nowy wariant (bez powtórek)
    }

    [Serializable]
    public class VariantGroup
    {
        [Tooltip("Nazwa stanu w Animatorze (dokładnie jak w Animatorze), np. Idle_Front")]
        public string stateName;

        [Tooltip("Klip bazowy użyty w tym stanie w bazowym Animator Controllerze")]
        public AnimationClip baseClip;

        [Tooltip("Warianty do losowania (min. 2)")]
        public List<AnimationClip> variants = new List<AnimationClip>();

        [Tooltip("Czy losować raz przy wejściu, czy cyklicznie po zakończeniu klipu")]
        public PickMode mode = PickMode.OnEnter;

        [NonSerialized] public int lastIndex = -1;
        [NonSerialized] public Coroutine cycleRoutine;
    }

    public Animator animator;
    public AnimatorOverrideController overrideController;
    public List<VariantGroup> groups = new List<VariantGroup>();

    int lastStateHash;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        // Zrób runtime kopię OverrideControllera, żeby nie modyfikować assetu
        if (overrideController != null)
        {
            var runtime = new AnimatorOverrideController(overrideController);
            overrideController = runtime;
            animator.runtimeAnimatorController = runtime;
        }
    }

    void Update()
    {
        var info = animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = info.shortNameHash;
        if (stateHash == lastStateHash) return;   // stan się nie zmienił
        lastStateHash = stateHash;

        // zatrzymaj wszystkie cykle (bo zmieniliśmy stan)
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            if (g.cycleRoutine != null)
            {
                StopCoroutine(g.cycleRoutine);
                g.cycleRoutine = null;
            }
        }

        // uruchom obsługę dla aktualnego stanu (jeśli jest w grupach)
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            if (!IsValid(g)) continue;
            if (!(info.IsName(g.stateName) || info.IsName("Base Layer." + g.stateName))) continue;

            if (g.mode == PickMode.OnEnter)
            {
                PickAndPlay(g);
            }
            else
            {
                g.cycleRoutine = StartCoroutine(CycleState(g));
            }
        }
    }

    IEnumerator CycleState(VariantGroup g)
    {
        while (true)
        {
            // jeśli wyszliśmy ze stanu - kończymy
            var s = animator.GetCurrentAnimatorStateInfo(0);
            if (!(s.IsName(g.stateName) || s.IsName("Base Layer." + g.stateName)))
                yield break;

            AnimationClip chosen = PickAndPlay(g);

            // czekamy aż klip się skończy (animator.speed może być różny)
            float speed = Mathf.Max(0.0001f, animator.speed);
            float wait = chosen.length / speed;

            yield return new WaitForSeconds(wait);
        }
    }

    AnimationClip PickAndPlay(VariantGroup g)
    {
        int next;
        do { next = UnityEngine.Random.Range(0, g.variants.Count); }
        while (g.variants.Count > 1 && next == g.lastIndex);

        g.lastIndex = next;

        AnimationClip chosen = g.variants[next];

        // podmień bazę na wariant
        overrideController[g.baseClip] = chosen;

        // NIE rób Rebind() ani Play() – bo resetują parametry i blend tree wraca do S
        animator.Update(0f);

        return chosen;
    }

    bool IsValid(VariantGroup g)
    {
        return overrideController != null
               && !string.IsNullOrEmpty(g.stateName)
               && g.baseClip != null
               && g.variants != null
               && g.variants.Count >= 2;
    }
}
