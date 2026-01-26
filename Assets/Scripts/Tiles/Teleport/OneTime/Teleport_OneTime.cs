using UnityEngine;

public class Teleport_OneTime : TeleportBase
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

    protected override bool CanTeleportFromThis() => true;
    protected override bool IsOneTime() => true;
}
