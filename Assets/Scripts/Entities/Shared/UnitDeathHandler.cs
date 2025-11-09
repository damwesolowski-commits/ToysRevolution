using UnityEngine;

[RequireComponent(typeof(Health))]
public class UnitDeathHandler : MonoBehaviour
{
    private Health health;
    private bool isDead;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} został zniszczony.");

        // 🔹 Wyłącz komponenty ruchu i interakcji
        var mover = GetComponent<GridMover>();
        if (mover != null) mover.enabled = false;

        var clickMove = GetComponent<ClickToMove2D>();
        if (clickMove != null) clickMove.enabled = false;

        var selector = GetComponent<SelectableHighlight>();
        if (selector != null) selector.enabled = false;

        // 🔹 Opcjonalnie efekt śmierci (zniknięcie po sekundzie)
        Destroy(gameObject, 1f);
    }
}
