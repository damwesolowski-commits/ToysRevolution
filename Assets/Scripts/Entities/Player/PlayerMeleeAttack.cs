using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Parametry ataku")]
    public int damage = 10;
    public float cooldown = 1.5f;
    public float attackRange = 1.6f;

    [Header("Ruch podczas gonienia przeciwnika")]
    public GridMover gridMover;        // przeciągnij tu komponent GridMover z Playera
    [Tooltip("Jak bardzo musi zmienić się pozycja celu, aby przeliczyć ścieżkę")]
    public float reChaseDistance = 0.2f;

    [Header("Z kim Player może walczyć")]
    public LayerMask enemyLayers;

    [Header("Opóźnienie po otrzymaniu ciosu")]
    public float hitReactionDelay = 1f;

    // stan walki
    private Transform currentTarget;
    private Health currentTargetHealth;
    private bool inCombat = false;
    private float lastAttackTime = -999f;
    private UnitInventory inventory;

    // Czy w tym trybie walki możemy gonić cel,
    // gdy wyjdzie poza attackRange?
    private bool chaseWhenOutOfRange = true;

    // zapamiętana pozycja celu, do której ostatnio liczyliśmy path
    private Vector3 lastChaseTarget;

    // nasze własne zdrowie – potrzebne, żeby reagować na otrzymany cios
    private Health selfHealth;

    private float lastHitTime = -Mathf.Infinity;

    [Header("UI – cooldown ataku")]
    [SerializeField] private CooldownBar cooldownBar;

    private void Awake()
    {
        inventory = GetComponent<UnitInventory>();
        selfHealth = GetComponent<Health>();

        Debug.Log("[PlayerMeleeAttack] Awake() - selfHealth = " + selfHealth);

        if (selfHealth != null)
        {
            // subskrypcja: ktoś nas uderzył
            selfHealth.OnDamagedBy += OnDamagedBy;

            Debug.Log("[PlayerMeleeAttack] Subskrybuję OnDamagedBy");
        }
        else
        {
            Debug.LogError("[PlayerMeleeAttack] BRAK Health na tym obiekcie Playera!");
        }
    }


    private void OnDestroy()
    {
        if (selfHealth != null)
        {
            selfHealth.OnDamagedBy -= OnDamagedBy;
        }
    }

    void Update()
    {
        if (!inCombat) return;

        // jeśli nie mamy celu albo cel umarł → koniec walki
        if (currentTarget == null || currentTargetHealth == null || currentTargetHealth.CurrentHP <= 0)
        {
            StopCombat();
            return;
        }

        // ✅ najpierw pobierz staty (z broni albo fallback "pięści")
        GetAttackStats(out int finalDamage, out float finalCooldown, out float finalRange);

        // odległość do celu
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // jeśli jesteśmy poza zasięgiem (użyj finalRange)
        if (distance > finalRange)
        {
            if (chaseWhenOutOfRange)
            {
                ChaseTarget();
            }
            else
            {
                StopCombat();
            }
            return;
        }

        // jesteśmy w zasięgu → jeśli minął cooldown (użyj finalCooldown)
        if (Time.time >= lastAttackTime + finalCooldown &&
            Time.time >= lastHitTime + hitReactionDelay)
        {
            DoAttack();
        }
    }

    /// <summary>
    /// Pomocniczo: zatrzymaj ruch i wyczyść zapamiętane komendy GridMovera.
    /// Używamy tego przy każdym wejściu/wyjściu z trybu walki.
    /// </summary>
    private void CancelMovement()
    {
        if (gridMover == null) return;

        // zatrzymaj aktualny ruch (z zachowaniem niepodzielności kroku A→B)
        gridMover.StopMoving();

        // skasuj wszystkie zapamiętane pendingCommand / stare cele
        gridMover.ClearQueuedCommands();
    }

    /// <summary>
    /// Rozpocznij tryb walki z podanym celem
    /// </summary>
    public void StartCombat(Transform target, bool allowChase)
    {
        if (target == null) return;

        // Wejście w walkę zawsze przerywa wcześniejszy rozkaz ruchu
        CancelMovement();

        currentTarget = target;
        currentTargetHealth = target.GetComponent<Health>();

        if (currentTargetHealth == null)
        {
            currentTarget = null;
            return;
        }

        // ustawiamy, czy ta walka ma prawo gonić poza zasięg
        chaseWhenOutOfRange = allowChase;

        inCombat = true;
        lastChaseTarget = currentTarget.position;
    }

    // stara wersja jako wygodny wrapper – domyślnie z pościgiem
    public void StartCombat(Transform target)
    {
        StartCombat(target, true);
    }

    /// <summary>
    /// Zatrzymaj tryb walki (np. przy nowym rozkazie ruchu)
    /// </summary>
    public void StopCombat()
    {
        inCombat = false;

        currentTarget = null;
        currentTargetHealth = null;

        // wyczyść ruch po zakończeniu walki
        CancelMovement();
    }

    private void DoAttack()
    {
        // bierzemy staty z broni (lub pięści)
        GetAttackStats(out int finalDamage, out float finalCooldown, out float finalRange);

        // jeśli cel żyje → zadaj obrażenia
        if (currentTargetHealth != null && currentTargetHealth.CurrentHP > 0)
        {
            currentTargetHealth.TakeDamage(finalDamage, transform);

            // start paska cooldownu
            if (cooldownBar != null)
            {
                cooldownBar.StartCooldown(finalCooldown);
            }

            // zapamiętaj czas ataku (musi być na końcu)
            lastAttackTime = Time.time;
        }
    }

    private void GetAttackStats(out int finalDamage, out float finalCooldown, out float finalRange)
    {
        // fallback: wartości "bez broni" z inspektora
        finalDamage = damage;
        finalCooldown = cooldown;
        finalRange = attackRange;

        if (inventory == null) inventory = GetComponent<UnitInventory>();
        if (inventory == null) return;

        var weapon = inventory.EquippedWeapon;
        if (weapon == null) return;

        // broń nadpisuje staty
        finalDamage = weapon.damage;
        finalCooldown = weapon.cooldownSeconds;
        finalRange = weapon.range;
    }

    private void ChaseTarget()
    {
        if (gridMover == null || currentTarget == null)
            return;

        Vector3 targetPos = currentTarget.position;

        // jeśli nie idziemy albo cel wyraźnie się przesunął → licz nową ścieżkę
        if (!gridMover.IsMoving() || Vector3.Distance(lastChaseTarget, targetPos) > reChaseDistance)
        {
            lastChaseTarget = targetPos;
            gridMover.RequestPathTo(targetPos);
        }
    }

    /// <summary>
    /// Reakcja na otrzymanie obrażeń
    /// </summary>
    private void OnDamagedBy(Transform attacker)
    {
        Debug.Log($"[PlayerMeleeAttack] OnDamagedBy od: {attacker?.name}, layer={attacker?.gameObject.layer}");
        if (attacker == null) return;

        // czy to wróg?
        if ((enemyLayers.value & (1 << attacker.gameObject.layer)) == 0)
            return;

        // 🔵 Zarejestruj moment otrzymania ciosu – od teraz przez
        // hitReactionDelay sekund NIE możemy wykonać swojego ataku.
        lastHitTime = Time.time;
        Debug.Log("[PlayerMeleeAttack] lastHitTime ustawiony = " + lastHitTime);

        // 🔒 Jeśli JUŻ jesteśmy w trybie walki (z kimkolwiek),
        // NIE zmieniamy celu – priorytet ma aktualny target / rozkaz gracza.
        if (inCombat)
            return;

        // 📏 dystans do napastnika
        float dist = Vector2.Distance(transform.position, attacker.position);

        GetAttackStats(out _, out _, out float finalRange);

        if (dist <= finalRange)
        {
            StartCombat(attacker, false);
        }
    }
}
