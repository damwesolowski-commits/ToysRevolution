using UnityEngine;

public abstract class GridPickupBase : MonoBehaviour
{
    private Vector2Int registeredTile;
    private bool registered = false;

    private void OnEnable()
    {
        TryRegister();
    }

    protected virtual void Start()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        Unregister();
        CancelInvoke(nameof(TryRegister));
    }

    protected virtual void OnDestroy()
    {
        Unregister();
        CancelInvoke(nameof(TryRegister));
    }

    private void TryRegister()
    {
        if (registered) return;

        if (TileLogicManager.Instance == null || GridPickupManager.Instance == null)
        {
            // Spróbuj ponownie za chwilę (kolejność Awake/OnEnable bywa różna)
            Invoke(nameof(TryRegister), 0.1f);
            return;
        }

        registeredTile = TileLogicManager.Instance.WorldToGrid(transform.position);
        GridPickupManager.Instance.Register(this, registeredTile);
        registered = true;

        CancelInvoke(nameof(TryRegister));
    }

    private void Unregister()
    {
        if (!registered) return;
        if (GridPickupManager.Instance == null) return;

        GridPickupManager.Instance.Unregister(this, registeredTile);
        registered = false;
    }

    public bool TryAutoPickup(GameObject unit)
    {
        if (unit == null) return false;
        return OnTryPickup(unit);
    }

    protected abstract bool OnTryPickup(GameObject unit);
}
