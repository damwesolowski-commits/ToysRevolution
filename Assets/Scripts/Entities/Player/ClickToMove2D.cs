using UnityEngine;
using Pathfinding;

public class ClickToMove2D : MonoBehaviour
{
    [SerializeField] private GridMover gridMover;                  // ruch tej jednostki
    [SerializeField] private PlayerMeleeAttack playerMeleeAttack;  // jej atak
    [SerializeField] private SelectableHighlight selectable;       // info, czy jest zaznaczona

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        if (gridMover == null)
            gridMover = GetComponent<GridMover>();

        if (playerMeleeAttack == null)
            playerMeleeAttack = GetComponent<PlayerMeleeAttack>();

        if (selectable == null)
            selectable = GetComponent<SelectableHighlight>();
    }

    private void Update()
    {
        // reagujemy tylko na PPM
        if (!Input.GetMouseButtonDown(1))
            return;

        // TYLKO zaznaczona jednostka przyjmuje rozkaz
        if (selectable != null && !selectable.IsSelected)
            return;

        Vector3 clickPos = GetWorldClickPosition();

        // 1) Najpierw spróbuj wydać rozkaz ataku
        if (TryIssueAttackCommand(clickPos))
            return;

        // 2) Jeśli nie kliknęliśmy w wroga → rozkaz ruchu
        IssueMoveCommand(clickPos);
    }

    /// <summary>
    /// Zwraca pozycję kliknięcia w świecie (z=0).
    /// </summary>
    private Vector3 GetWorldClickPosition()
    {
        Vector3 clickPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        clickPos.z = 0f;
        return clickPos;
    }

    /// <summary>
    /// Jeśli kliknięto w wroga, wydaje rozkaz ataku i zwraca true.
    /// </summary>
    private bool TryIssueAttackCommand(Vector3 clickPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(clickPos, Vector2.zero);
        if (hit.collider == null)
            return false;

        bool isEnemy =
            hit.collider.CompareTag("Enemy") ||
            hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemies");

        if (!isEnemy)
            return false;

        var enemyHealth = hit.collider.GetComponentInParent<Health>();
        if (enemyHealth != null && playerMeleeAttack != null)
        {
            // NOWY ROZKAZ ATAKU.
            // StartCombat samo zatrzyma ruch i wyczyści kolejkę w GridMoverze.
            playerMeleeAttack.StartCombat(enemyHealth.transform);
        }

        // przy rozkazie ataku NIE wydajemy rozkazu ruchu – ściganiem zajmuje się PlayerMeleeAttack
        return true;
    }

    /// <summary>
    /// Wydaje zwykły rozkaz ruchu do klikniętej pozycji.
    /// </summary>
    private void IssueMoveCommand(Vector3 clickPos)
    {
        // przerwij ewentualny tryb walki (StopCombat zatrzyma ruch i wyczyści kolejkę)
        if (playerMeleeAttack != null)
        {
            playerMeleeAttack.StopCombat();
        }

        if (gridMover == null)
            return;

        var p = clickPos;

        // snap do środka kafelka
        p.x = Mathf.Floor(p.x) + 0.5f;
        p.y = Mathf.Floor(p.y) + 0.5f;

        // dopasuj do najbliższego walkable node'a w grafie A*
        var nn = AstarPath.active.GetNearest(p, NNConstraint.Default);
        if (nn.node != null && nn.node.Walkable)
            p = (Vector3)nn.position;

        // bezpośrednio każemy GridMoverowi iść w to miejsce
        gridMover.RequestPathTo(p);
    }
}
