using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class LoadPatrolPointsFromChildren : Action
{
    public SharedGameObject patrolRoot;
    public SharedGameObjectList patrolPoints;

    public bool failIfMissing = false;
    public bool detachPointsToWorld = true;
    public SharedGameObject worldParent;

    private bool _initialized = false;

    public override void OnStart()
    {
        if (_initialized) return;

        if (patrolPoints == null)
        {
            _initialized = true;
            return;
        }

        if (patrolPoints.Value == null) patrolPoints.Value = new List<GameObject>();
        else patrolPoints.Value.Clear();

        Transform root = null;

        if (patrolRoot != null && patrolRoot.Value != null)
            root = patrolRoot.Value.transform;
        else
        {
            var t = transform.Find("PatrolPoints");
            if (t != null) root = t;
        }

        if (root == null)
        {
            _initialized = true;
            return;
        }

        for (int i = 0; i < root.childCount; i++)
            patrolPoints.Value.Add(root.GetChild(i).gameObject);

        if (detachPointsToWorld)
        {
            Transform newParent = null;
            if (worldParent != null && worldParent.Value != null)
                newParent = worldParent.Value.transform;

            foreach (var p in patrolPoints.Value)
                p.transform.SetParent(newParent, true);
        }

        _initialized = true;
    }

    public override TaskStatus OnUpdate()
    {
        if (patrolPoints == null) return TaskStatus.Failure;

        // jeśli nie ma punktów:
        if (patrolPoints.Value == null || patrolPoints.Value.Count == 0)
            return failIfMissing ? TaskStatus.Failure : TaskStatus.Success;

        return TaskStatus.Success;
    }
}
