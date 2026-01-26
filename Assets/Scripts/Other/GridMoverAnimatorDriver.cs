using UnityEngine;

[RequireComponent(typeof(GridMover))]
public class GridMoverAnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator parameter names")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";

    [Header("Tuning")]
    [Tooltip("Jak mały ruch na klatkę uznajemy za 'zero' (żeby nie gubić kierunku).")]
    [SerializeField] private float deltaEps = 0.00005f;

    private GridMover mover;
    private Vector3 lastPos;

    // ostatni sensowny kierunek (dla idle + gdy delta chwilowo 0)
    private Vector2Int lastNonZeroDir = new Vector2Int(0, -1); // S

    private void Awake()
    {
        mover = GetComponent<GridMover>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        lastPos = transform.position;
    }

    private void Update()
    {
        if (animator == null || mover == null) return;

        // 1) SPEED bierzemy z GridMover -> nie będzie skakać 1/0 w trakcie ruchu
        bool isMoving = mover.IsMoving();
        animator.SetFloat(speedParam, isMoving ? 1f : 0f);

        // 2) Kierunek: delta daje natychmiastowy kierunek (bez opóźnienia o 1 kafelek)
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;

        if (isMoving)
        {
            int dx = Mathf.Abs(delta.x) > deltaEps ? (int)Mathf.Sign(delta.x) : 0;
            int dy = Mathf.Abs(delta.y) > deltaEps ? (int)Mathf.Sign(delta.y) : 0;

            // jeśli w tej klatce delta wyszła 0/0, nie psuj kierunku — zostaw poprzedni
            if (dx != 0 || dy != 0)
                lastNonZeroDir = new Vector2Int(dx, dy);
        }
        else
        {
            // gdy stoimy, możesz (opcjonalnie) zsynchronizować z tym co GridMover pamięta
            // var d = mover.GetLastDirection();
            // if (d != Vector2Int.zero) lastNonZeroDir = d;
        }

        animator.SetFloat(moveXParam, lastNonZeroDir.x);
        animator.SetFloat(moveYParam, lastNonZeroDir.y);

        lastPos = pos;
    }
}
