using UnityEngine;
using Pathfinding;  // <= DODAJ

public class ClickToMove2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    Camera mainCam;

    void Start() => mainCam = Camera.main;

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && target != null)
        {
            var p = mainCam.ScreenToWorldPoint(Input.mousePosition);
            p.z = 0f;

            // 🔙 klasyczny snap do środka kafla
            p.x = Mathf.Floor(p.x) + 0.5f;
            p.y = Mathf.Floor(p.y) + 0.5f;

            var nn = AstarPath.active.GetNearest(p, NNConstraint.Default);
            if (nn.node != null && nn.node.Walkable)
                p = (Vector3)nn.position;

            target.position = p;
        }
    }
}
