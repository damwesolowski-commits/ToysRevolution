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
            p.z = 0;

            // zatrzaśnij w środek kratki
            p.x = Mathf.Floor(p.x) + 0.5f;
            p.y = Mathf.Floor(p.y) + 0.5f;

            // ✨ Najbliższy W A L K A B L E węzeł (jeśli klik był na niechodliwym)
            var nn = AstarPath.active.GetNearest(p, NNConstraint.Default);
            if (nn.node != null && nn.node.Walkable)
                p = (Vector3)nn.position;

            target.position = p;
        }
    }
}
