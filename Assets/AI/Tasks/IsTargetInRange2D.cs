using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class IsTargetInRange2D : Conditional
{
    public SharedGameObject target;
    public SharedFloat detectionRange;

    // Opcjonalne: możesz zostawić, ale nie musisz ustawiać w BD
    public SharedGameObject gridObject;

    private Grid _grid;

    public override void OnAwake()
    {
        ResolveGrid();
    }

    public override TaskStatus OnUpdate()
    {
        if (target == null || target.Value == null) return TaskStatus.Failure;

        if (_grid == null) ResolveGrid();
        if (_grid == null) return TaskStatus.Failure;

        Vector3Int myCell = _grid.WorldToCell(transform.position);
        Vector3Int targetCell = _grid.WorldToCell(target.Value.transform.position);

        Vector2 myCenter = (Vector2)_grid.GetCellCenterWorld(myCell);
        Vector2 targetCenter = (Vector2)_grid.GetCellCenterWorld(targetCell);

        float distance = Vector2.Distance(myCenter, targetCenter);
        return distance <= detectionRange.Value ? TaskStatus.Success : TaskStatus.Failure;
    }

    private void ResolveGrid()
    {
        // 1) Jeśli ktoś jednak podał Grid Object (np. na instancji), użyj go
        if (gridObject != null && gridObject.Value != null)
        {
            _grid = gridObject.Value.GetComponent<Grid>();
            if (_grid != null) return;
        }

        // 2) W przeciwnym razie znajdź Grid na scenie automatycznie
        _grid = Object.FindObjectOfType<Grid>();
    }
}
