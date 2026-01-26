using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

public class IsAttackCooldownActive : Conditional
{
    private CooldownBar cooldownBar;

    public override void OnAwake()
    {
        cooldownBar = gameObject.GetComponentInChildren<CooldownBar>();
    }

    public override TaskStatus OnUpdate()
    {
        if (cooldownBar == null)
            return TaskStatus.Failure;

        return cooldownBar.IsCooldownActive()
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
