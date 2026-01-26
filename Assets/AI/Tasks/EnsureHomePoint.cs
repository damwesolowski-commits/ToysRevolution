using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

/// <summary>
/// Tworzy (jeśli nie istnieje) niewidzialny punkt "Home" i zapisuje go do SharedGameObject.
/// Dzięki temu możemy używać istniejącego MoveToTargetOnGrid do powrotu.
/// </summary>
public class EnsureHomePoint : Action
{
    public SharedGameObject homePoint;

    [UnityEngine.Tooltip("Jeśli true, HomePoint będzie dzieckiem Enemiego (porusza się razem z nim) - ZOSTAW FALSE.")]
    public bool parentToEnemy = false;

    [UnityEngine.Tooltip("Nazwa tworzonego obiektu HomePoint (dla porządku w Hierarchy).")]
    public string homeName = "HomePoint";

    public override void OnStart()
    {
        if (homePoint != null && homePoint.Value != null)
            return;

        var go = new GameObject($"{gameObject.name}_{homeName}");
        go.transform.position = transform.position;

        if (parentToEnemy)
            go.transform.SetParent(transform, true);

        homePoint.Value = go;
    }

    public override TaskStatus OnUpdate()
    {
        return (homePoint != null && homePoint.Value != null) ? TaskStatus.Success : TaskStatus.Failure;
    }
}

