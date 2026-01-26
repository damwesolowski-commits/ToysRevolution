using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class IsAtHome2D : Conditional
{
    public SharedGameObject homePoint;

    [UnityEngine.Tooltip("Dystans uznawany za 'jestem w domu'. Daj 0.1-0.3 albo 0.5 jeśli chcesz luz.")]
    public float homeDistance = 0.2f;

    public override TaskStatus OnUpdate()
    {
        if (homePoint == null || homePoint.Value == null)
            return TaskStatus.Failure;

        float dist = Vector2.Distance(transform.position, homePoint.Value.transform.position);
        return dist <= homeDistance ? TaskStatus.Success : TaskStatus.Failure;
    }
}
