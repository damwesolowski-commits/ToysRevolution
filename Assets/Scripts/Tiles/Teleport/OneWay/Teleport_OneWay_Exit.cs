using UnityEngine;

public class Teleport_OneWay_Exit : TeleportBase
{
    private Teleport_OneWayRegistry _registry;

    protected override TeleportRegistryBase Registry
    {
        get
        {
            if (_registry == null)
                _registry = FindFirstObjectByType<Teleport_OneWayRegistry>();
            return _registry;
        }
    }

    // Ten teleport NIE może być aktywowany wejściem, tylko jako wyjście
    protected override bool CanTeleportFromThis() => false;

    // Wyjście NIE znika po użyciu
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
