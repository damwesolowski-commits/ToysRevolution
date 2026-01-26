using UnityEngine;

public class Teleport_OneWay : TeleportBase
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

    protected override bool CanTeleportFromThis() => true; // wejść można
    protected override bool IsOneTime() => false;
}
