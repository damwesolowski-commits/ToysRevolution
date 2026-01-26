using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class IsWithinLeashRange2D : Conditional
{
    public SharedGameObject homePoint;
    public SharedGameObject target; // <- PLAYER

    [UnityEngine.Tooltip("Maksymalna odległość TARGET (np. Playera) od HomePoint (smycz). Jeśli target dalej -> Failure.")]
    public SharedFloat leashRange;

    public override TaskStatus OnUpdate()
    {
        if (homePoint == null || homePoint.Value == null) return TaskStatus.Failure;
        if (target == null || target.Value == null) return TaskStatus.Failure;

        float range = leashRange != null ? leashRange.Value : 0f;

        // Smycz liczona od HOME do PLAYERA
        float dist = Vector2.Distance(
            (Vector2)target.Value.transform.position,
            (Vector2)homePoint.Value.transform.position
        );

        return dist <= range ? TaskStatus.Success : TaskStatus.Failure;
    }
}
