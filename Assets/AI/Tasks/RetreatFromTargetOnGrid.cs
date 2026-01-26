using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[RequireComponent(typeof(GridMover))]
public class RetreatFromTargetOnGrid : Action
{
    public SharedGameObject target;      // Player
    public SharedGameObject homePoint;   // HomePoint
    public SharedFloat leashRange;       // ten sam co w IsWithinLeashRange2D

    [UnityEngine.Tooltip("Jak daleko (w world units / kafelkach) próbujemy uciekać w jednym 'skoku'.")]
    public float fleeStepDistance = 3f;

    [UnityEngine.Tooltip("Margines, żeby nie próbować iść dokładnie po krawędzi leash.")]
    public float leashMargin = 0.25f;

    [UnityEngine.Tooltip("Kiedy uznajemy, że doszliśmy do punktu ucieczki.")]
    public float arriveDistance = 0.2f;

    [UnityEngine.Tooltip("Jak mocno musi zmienić się punkt ucieczki, aby przeliczyć ścieżkę.")]
    public float repathDistance = 0.5f;

    private GridMover mover;
    private Vector3 lastDestination;
    private bool pathRequested;

    public override void OnAwake()
    {
        mover = GetComponent<GridMover>();
    }

    public override void OnStart()
    {
        pathRequested = false;

        if (mover == null) return;
        if (target == null || target.Value == null) return;
        if (homePoint == null || homePoint.Value == null) return;

        RequestNewPath();
    }

    public override TaskStatus OnUpdate()
    {
        if (mover == null) return TaskStatus.Failure;
        if (target == null || target.Value == null) return TaskStatus.Failure;
        if (homePoint == null || homePoint.Value == null) return TaskStatus.Failure;

        // Jeśli jesteśmy blisko celu ucieczki – sukces (BT i tak wywoła to ponownie,
        // dopóki cooldown trwa, więc będzie generować kolejne cele).
        float distToDest = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.y),
            new Vector2(lastDestination.x, lastDestination.y)
        );

        if (pathRequested && distToDest <= arriveDistance)
        {
            mover.StopMoving();
            return TaskStatus.Success;
        }

        // Co tick licz nowy cel i przelicz ścieżkę, jeśli istotnie się zmienił
        Vector3 desiredDest = ComputeFleeDestination();
        float diff = Vector2.Distance(
            new Vector2(desiredDest.x, desiredDest.y),
            new Vector2(lastDestination.x, lastDestination.y)
        );

        if (!pathRequested || diff > repathDistance)
        {
            lastDestination = desiredDest;
            pathRequested = true;
            mover.RequestPathTo(lastDestination);
        }

        return TaskStatus.Running;
    }

    private void RequestNewPath()
    {
        lastDestination = ComputeFleeDestination();
        pathRequested = true;
        mover.RequestPathTo(lastDestination);
    }

    // --- NOWA LOGIKA: wybór najlepszego kierunku ucieczki, także przy brzegu leash ---
    private Vector3 ComputeFleeDestination()
    {
        Vector3 myPos = transform.position;
        Vector3 playerPos = target.Value.transform.position;

        Vector2 away = (Vector2)(myPos - playerPos);
        if (away.sqrMagnitude < 0.0001f) away = Vector2.right;
        away.Normalize();

        Vector2 home = homePoint.Value.transform.position;

        float leash = leashRange != null ? leashRange.Value : 0f;
        float maxRadius = Mathf.Max(0f, leash - leashMargin);

        float distFromHome = Vector2.Distance((Vector2)myPos, home);
        bool nearBoundary = distFromHome >= (maxRadius - 0.05f);

        Vector2[] dirs;
        if (!nearBoundary)
        {
            dirs = new[]
            {
                away,
                Rotate(away, 45f),
                Rotate(away, -45f),
                Rotate(away, 90f),
                Rotate(away, -90f),
                Rotate(away, 135f),
                Rotate(away, -135f),
            };
        }
        else
        {
            // Na brzegu: preferuj ruch po stycznej (wzdłuż okręgu leash)
            Vector2 radial = ((Vector2)myPos - home);
            if (radial.sqrMagnitude < 0.0001f) radial = away;
            radial.Normalize();

            Vector2 tangentA = new Vector2(-radial.y, radial.x);
            Vector2 tangentB = -tangentA;

            float scoreA = Vector2.Distance((Vector2)myPos + tangentA, (Vector2)playerPos);
            float scoreB = Vector2.Distance((Vector2)myPos + tangentB, (Vector2)playerPos);

            Vector2 bestTangent = scoreA >= scoreB ? tangentA : tangentB;

            dirs = new[]
            {
                bestTangent,
                Rotate(bestTangent, 45f),
                Rotate(bestTangent, -45f),
                away // awaryjnie
            };
        }

        Vector3 best = myPos;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2 dir = dirs[i];
            if (dir.sqrMagnitude < 0.0001f) continue;
            dir.Normalize();

            Vector3 rawDest = myPos + (Vector3)(dir * fleeStepDistance);
            rawDest.z = myPos.z;

            Vector3 clamped = ClampToLeash(rawDest, home, maxRadius, myPos.z);

            float distToPlayer = Vector2.Distance((Vector2)clamped, (Vector2)playerPos);
            float moveDelta = Vector2.Distance((Vector2)clamped, (Vector2)myPos);

            // im dalej od playera, tym lepiej + lekka preferencja na faktyczny ruch
            float score = distToPlayer + moveDelta * 0.25f;

            if (score > bestScore)
            {
                bestScore = score;
                best = clamped;
            }
        }

        return best;
    }

    private static Vector3 ClampToLeash(Vector3 rawDest, Vector2 home, float maxRadius, float z)
    {
        Vector2 raw2 = rawDest;
        Vector2 fromHome = raw2 - home;
        float dist = fromHome.magnitude;

        if (dist > maxRadius && dist > 0.0001f)
            raw2 = home + fromHome.normalized * maxRadius;

        return new Vector3(raw2.x, raw2.y, z);
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
