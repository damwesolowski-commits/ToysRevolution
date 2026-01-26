using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class SetNextPatrolPointLoop : Action
{
    public SharedGameObjectList patrolPoints;
    public SharedInt patrolIndex;
    public SharedGameObject patrolTarget;

    public override TaskStatus OnUpdate()
    {
        if (patrolPoints == null || patrolPoints.Value == null || patrolPoints.Value.Count == 0)
            return TaskStatus.Failure;

        int count = patrolPoints.Value.Count;

        // zabezpieczenie na wypadek śmieciowej wartości
        int idx = patrolIndex != null ? patrolIndex.Value : 0;
        if (idx < 0) idx = 0;

        // ZAWIJANIE PĘTLI:
        idx = idx % count;

        // ustaw target
        var go = patrolPoints.Value[idx];
        if (go == null) return TaskStatus.Failure;

        patrolTarget.Value = go;

        // przygotuj indeks na następny raz
        patrolIndex.Value = (idx + 1) % count;

        return TaskStatus.Success;
    }
}
