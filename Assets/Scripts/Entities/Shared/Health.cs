using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [field: SerializeField] public int MaxHP { get; private set; } = 100;
    public int CurrentHP { get; private set; }

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnDied;
    public System.Action<Transform> OnDamagedBy;

    public event Action OnDamaged;   // WYWOŁYWANE ZA KAŻDYM RAZEM, GDY JEDNOSTKA OTRZYMA OBRAŻENIA

    // referencja do paska zdrowia
    private HealthBar healthBar;

    void Awake()
    {
        CurrentHP = MaxHP;

        // znajdź pasek zdrowia nawet jeśli jest wyłączony (ale GameObject ma być aktywny)
        healthBar = GetComponentInChildren<HealthBar>(true);
    }

    public void TakeDamage(int amount, Transform attacker = null)
    {
        if (amount <= 0 || CurrentHP <= 0) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);

        // POWIADOMIENIE, ŻE JEDNOSTKA OTRZYMAŁA OBRAŻENIA
        OnDamaged?.Invoke();

        // powiadom subskrybentów, kto nas uderzył
        if (attacker != null)
        {
            OnDamagedBy?.Invoke(attacker);
        }

        if (CurrentHP == 0)
        {
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHP <= 0) return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);

        // (opcjonalnie) jeśli chcesz też pokazywać pasek przy leczeniu:
        // if (healthBar != null) healthBar.Show();
    }
}
