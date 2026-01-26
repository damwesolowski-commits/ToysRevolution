using UnityEngine;

public class Teleport_MysteryExit : TeleportBase
{
    private Teleport_MysteryRegistry _registry;

    protected override TeleportRegistryBase Registry
    {
        get
        {
            if (_registry == null)
                _registry = FindFirstObjectByType<Teleport_MysteryRegistry>();
            return _registry;
        }
    }

    // Z wyjścia nie można się teleportować
    protected override bool CanTeleportFromThis() => false;

    // Wyjście nie jest jednorazowe – może być użyte wiele razy jako cel
    protected override bool IsOneTime() => false;

    // Może być celem teleportu
    public override bool CanBeExit() => true;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetExitCenter(), Vector3.one * 0.5f);
        Gizmos.DrawIcon(GetExitCenter(), "sv_label_2.png", true);
    }
#endif
}
