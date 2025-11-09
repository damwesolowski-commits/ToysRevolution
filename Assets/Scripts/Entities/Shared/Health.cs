using UnityEngine;

public class Health : MonoBehaviour
{
    [field: SerializeField] public int MaxHP { get; private set; } = 100;
    public int CurrentHP { get; private set; }

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnDied;

    void Awake() => CurrentHP = MaxHP;

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHP <= 0) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        if (CurrentHP == 0) OnDied?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHP <= 0) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }
}
