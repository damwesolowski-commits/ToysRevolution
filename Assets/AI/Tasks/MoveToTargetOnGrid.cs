using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// Wymagamy GridMovera na tym obiekcie
[RequireComponent(typeof(GridMover))]
public class MoveToTargetOnGrid : Action
{
    // Do kogo idziemy (np. najbliższy Player)
    public SharedGameObject target;

    // Jaka odległość oznacza "doszedłem"
    public float arriveDistance = 1f;       // przy kafelku 1x1 – 1 oznacza "obok"

    // O ile musi zmienić się pozycja celu, żebyśmy przeliczyli ścieżkę na nowo
    public float repathDistance = 0.5f;     // pół kafelka

    private GridMover mover;
    private bool pathRequested;

    // Ostatni punkt, do którego liczyliśmy ścieżkę
    private Vector3 lastDestination;

    public override void OnAwake()
    {
        mover = GetComponent<GridMover>();
    }

    public override void OnStart()
    {
        pathRequested = false;

        if (mover == null || target == null || target.Value == null)
            return;

        RequestNewPath();
    }

    public override TaskStatus OnUpdate()
    {
        if (mover == null || target == null || target.Value == null || !pathRequested)
            return TaskStatus.Failure;

        // Jeśli jesteśmy już wystarczająco blisko celu → sukces
        float distToTarget = Vector2.Distance(
            new Vector2(mover.transform.position.x, mover.transform.position.y),
            new Vector2(target.Value.transform.position.x, target.Value.transform.position.y)
        );

        if (distToTarget <= arriveDistance)
        {
            mover.StopMoving();
            return TaskStatus.Success;
        }

        // Jeżeli cel się przesunął → przelicz ścieżkę
        Vector3 currentTargetPos = target.Value.transform.position;
        currentTargetPos.z = mover.transform.position.z;

        float destDiff = Vector2.Distance(
            new Vector2(currentTargetPos.x, currentTargetPos.y),
            new Vector2(lastDestination.x, lastDestination.y)
        );

        if (destDiff > repathDistance)
        {
            RequestNewPath();
        }

        if (mover.IsMoving())
            return TaskStatus.Running;

        // GridMover stoi – sprawdź dystans do ostatniego destination
        float dist = Vector2.Distance(
            new Vector2(mover.transform.position.x, mover.transform.position.y),
            new Vector2(lastDestination.x, lastDestination.y)
        );

        if (dist <= arriveDistance)
            return TaskStatus.Success;

        // Jeśli nie doszliśmy (np. zablokowane) → spróbuj ponownie
        RequestNewPath();
        return TaskStatus.Running;
    }

    private void RequestNewPath()
    {
        Vector3 dest = target.Value.transform.position;
        dest.z = mover.transform.position.z;

        lastDestination = dest;
        pathRequested = true;

        mover.RequestPathTo(dest);
    }
}
