using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class MeleeAttack : Action
{
    // Cel ataku – ten sam SharedGameObject co w MoveToTargetOnGrid
    public SharedGameObject target;

    // Obrażenia, gdy Enemy nie ma broni
    public int damage = 10;

    // Czas odnowienia ataku (sekundy)
    public float cooldown = 3f;

    // Zasięg ataku – jeśli stoisz na gridzie 1x1, to 1.1f znaczy "w sąsiednim kafelku"
    public float attackRange = 1.6f;

    [Header("Opóźnienie po otrzymaniu ciosu")]
    public float hitReactionDelay = 1f;       // ile czasu po dostaniu ciosu Enemy nie może atakować

    private float lastAttackTime = -Mathf.Infinity; // kiedy ostatnio sam zaatakował
    private float lastHitTime = -Mathf.Infinity;    // kiedy ostatnio dostał cios

    private Health selfHealth;
    private UnitInventory inventory;

    // UI – cooldown ataku (tylko w kodzie, NIE w Behavior Designerze)
    private CooldownBar cooldownBar;

    // 🔹 Ruch Enemiego – zatrzymamy go, gdy zacznie atakować
    private GridMover gridMover;

    // Wywoływane raz, gdy Behavior Tree się budzi
    public override void OnAwake()
    {
        selfHealth = GetComponent<Health>();
        if (selfHealth != null)
        {
            selfHealth.OnDamagedBy += OnDamagedBy;
        }

        // nowość:
        gridMover = GetComponent<GridMover>();

        // 🔵 automatycznie znajdujemy CooldownBar w dzieciach Enemy
        var ownerTransform = transform;
        if (ownerTransform != null)
        {
            // true = szukaj także w wyłączonych obiektach
            cooldownBar = ownerTransform.GetComponentInChildren<CooldownBar>(true);
        }
        inventory = GetComponent<UnitInventory>();
    }

    // Porządek przy końcu życia noda / drzewa
    public override void OnEnd()
    {
        // NIC TU NIE ROBIMY z eventem
    }


    // Callback z Health: ktoś zadał temu Enemy obrażenia
    private void OnDamagedBy(Transform attacker)
    {
        // Nie przejmujemy się tym, kto – ważne, że Enemy dostał cios
        lastHitTime = Time.time;
    }

    /// <summary>
    /// Zatrzymaj wszelki ruch + skasuj zapamiętane komendy GridMovera.
    /// </summary>
    private void CancelMovement()
    {
        if (gridMover == null) return;

        gridMover.StopMoving();
        gridMover.ClearQueuedCommands();
    }

    public override TaskStatus OnUpdate()
    {
        if (target == null || target.Value == null)
            return TaskStatus.Failure;

        // 🔒 Bezpieczeństwo: jeśli z jakiegoś powodu jeszcze nie mamy referencji,
        // spróbuj znaleźć CooldownBar teraz (w dzieciach Enemy).
        if (cooldownBar == null)
        {
            var ownerTransform = transform;
            if (ownerTransform != null)
            {
                cooldownBar = ownerTransform.GetComponentInChildren<CooldownBar>(true);
            }
        }

        // Sprawdzamy, czy Player stoi w zasięgu ataku
        float dist = Vector2.Distance(transform.position, target.Value.transform.position);

        GetAttackStats(out int finalDamage, out float finalCooldown, out float finalRange);

        if (dist > finalRange)
        {
            return TaskStatus.Failure;
        }

        // 🔴 NOWOŚĆ: skoro jesteśmy w zasięgu ataku,
        // zatrzymaj WSZYSTKIE rozkazy ruchu Enemiego.
        CancelMovement();

        // Sprawdzamy cooldowny:
        if (Time.time < lastAttackTime + finalCooldown ||
            Time.time < lastHitTime + hitReactionDelay)
        {
            // Jeszcze trwa któryś z cooldownów – czekamy
            return TaskStatus.Running;
        }

        // Pobieramy komponent Health z Playera
        Health health = target.Value.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(finalDamage, transform);
            lastAttackTime = Time.time;

            // 🔵 cooldownbar dla Enemy
            if (cooldownBar != null)
            {
                cooldownBar.StartCooldown(finalCooldown);
            }
            else
            {
                Debug.LogWarning("[MeleeAttack] Enemy zadaje obrażenia, ale nie znalazłem CooldownBar w dzieciach.", gameObject);
            }
        }

        return TaskStatus.Success;
    }
    public bool IsAttackCooldownActive()
    {
        GetAttackStats(out _, out float finalCooldown, out _);
        return Time.time < lastAttackTime + finalCooldown;
    }

    public float AttackCooldownRemaining()
    {
        GetAttackStats(out _, out float finalCooldown, out _);
        return Mathf.Max(0f, (lastAttackTime + finalCooldown) - Time.time);
    }

    private void GetAttackStats(out int finalDamage, out float finalCooldown, out float finalRange)
    {
        finalDamage = damage;
        finalCooldown = cooldown;
        finalRange = attackRange;

        if (inventory == null) inventory = GetComponent<UnitInventory>();
        if (inventory == null) return;

        var weapon = inventory.EquippedWeapon;
        if (weapon == null) return;

        finalDamage = weapon.damage;
        finalCooldown = weapon.cooldownSeconds;
        finalRange = weapon.range;
    }
}
