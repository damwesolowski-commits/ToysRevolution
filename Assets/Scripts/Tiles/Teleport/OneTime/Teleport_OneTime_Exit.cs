using UnityEngine;

public class Teleport_OneTime_Exit : TeleportBase
{
    private Teleport_OneTimeRegistry _registry;

    protected override TeleportRegistryBase Registry
    {
        get
        {
            if (_registry == null)
                _registry = FindFirstObjectByType<Teleport_OneTimeRegistry>();
            return _registry;
        }
    }

    // Nie można aktywować tego teleportu poprzez wejście
    protected override bool CanTeleportFromThis() => false;

    // Wyjście NIE znika po użyciu — tylko Entry znika
    protected override bool IsOneTime() => false;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetExitCenter(), Vector3.one * 0.5f);

        Gizmos.DrawIcon(GetExitCenter(), "sv_label_4.png", true);
    }
#endif
}
