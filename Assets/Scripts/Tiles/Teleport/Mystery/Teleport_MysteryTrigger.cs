using System.Collections;
using UnityEngine;

public class Teleport_MysteryTrigger : TeleportBase
{
    private Teleport_MysteryRegistry _registry;

    [Header("Mystery Trigger Settings")]
    [Tooltip("Czy po aktywacji tego triggera ma zostać włączona globalna muzyka tajemniczego przejścia.")]
    public bool playMusicOnActivation = true;

    // Po użyciu jakiegokolwiek wejścia w tej grupie → trigger staje się nieaktywny na zawsze
    private bool permanentlyDisabled = false;

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

        // W edytorze chcesz widzieć sprite, ale w trakcie gry pole ma być niewidoczne
        if (Application.isPlaying && spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    protected override bool CanTeleportFromThis()
    {
        // "Teleport" w tym wypadku = aktywacja wejść
        return !permanentlyDisabled;
    }

    // Trigger nigdy się nie usuwa automatycznie
    protected override bool IsOneTime() => false;

    // Trigger nigdy nie może być wyjściem teleportu
    public override bool CanBeExit() => false;

    public void DisablePermanently()
    {
        permanentlyDisabled = true;
    }

    protected override IEnumerator TeleportRoutine(GameObject unit)
    {
        if (permanentlyDisabled)
            yield break;

        var reg = Registry as Teleport_MysteryRegistry;
        if (reg == null)
        {
            Debug.LogWarning("[Teleport_MysteryTrigger] Brak Teleport_MysteryRegistry w scenie.");
            yield break;
        }

        // 🔹 Aktywuj wszystkie wejścia w tej samej grupie
        reg.ActivateEntrancesForGroup(groupId, playMusicOnActivation);

        // Opcjonalny dźwięk wejścia na tajemnicze pole
        PlaySound(enterSound);

        // NIE teleportujemy jednostki, tylko uruchamiamy event
        yield break;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetExitCenter(), Vector3.one * 0.5f);
        Gizmos.DrawIcon(GetExitCenter(), "sv_label_0.png", true);
    }
#endif
}
