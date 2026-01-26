using UnityEngine;

public class Teleport_BiDirectional : TeleportBase
{
    private Teleport_BiDirectionalRegistry _registry;

    protected override TeleportRegistryBase Registry
    {
        get
        {
            if (_registry == null)
                _registry = FindFirstObjectByType<Teleport_BiDirectionalRegistry>();
            return _registry;
        }
    }

    protected override bool CanTeleportFromThis() => true;
    protected override bool IsOneTime() => false;
}
