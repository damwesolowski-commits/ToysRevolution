using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform selectionBox;

    [Header("Hit-detection")]
    [Tooltip("Warstwy z jednostkami (np. Units albo Player).")]
    [SerializeField] private LayerMask selectableMask;

    [Header("Debug")]
    public bool logDebug = false;

    private Vector2 startPos;
    private Camera mainCam;
    private readonly List<SelectableHighlight> selectedUnits = new List<SelectableHighlight>();
    private bool isDragging = false;
    private const float dragThreshold = 15f;

    void Start()
    {
        mainCam = Camera.main;

        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);

        if (selectableMask.value == 0)
            selectableMask = LayerMask.GetMask("Player");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            if (Vector2.Distance(startPos, Input.mousePosition) > dragThreshold)
            {
                if (!isDragging)
                {
                    isDragging = true;
                    if (selectionBox != null)
                    {
                        selectionBox.gameObject.SetActive(true);
                        UpdateBox(Input.mousePosition);
                    }
                }

                if (isDragging && selectionBox != null)
                    UpdateBox(Input.mousePosition);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                if (selectionBox != null)
                    selectionBox.gameObject.SetActive(false);

                DeselectAll();
                SelectUnitsInBox();
            }
            else
            {
                HandleClickSelect();
            }

            isDragging = false;
        }
    }

    // ---------------- CLICK SELECT ----------------

    private void HandleClickSelect()
    {
        Vector3 world = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(world.x, world.y);

        // Pobieramy WSZYSTKIE collidery pod kursorem
        Collider2D[] hits = Physics2D.OverlapPointAll(point, selectableMask);

        // 🔹 Jeśli kliknięto w puste pole — odznacz wszystko
        if (hits == null || hits.Length == 0)
        {
            if (logDebug) Debug.Log("Selection: no hits (clearing all)");
            DeselectAll();
            return;
        }

        // 🔹 Szukamy najwyżej położonego SelectableHighlight
        SelectableHighlight clicked = PickTopmostSelectable(hits);

        // 🔹 Jeżeli żaden SelectableHighlight nie znaleziony — też odznacz wszystko
        if (clicked == null)
        {
            if (logDebug) Debug.Log("Selection: no selectable found (clearing all)");
            DeselectAll();
            return;
        }

        // 🔹 Wypisanie debug hitów
        if (logDebug)
        {
            foreach (var h in hits)
                Debug.Log($"Selection hit: {h.name} (layer={LayerMask.LayerToName(h.gameObject.layer)})");
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shift)
        {
            // Multi-select (toggle)
            if (clicked.IsSelected)
            {
                clicked.SetSelected(false);
                selectedUnits.Remove(clicked);
            }
            else
            {
                clicked.SetSelected(true);
                if (!selectedUnits.Contains(clicked))
                    selectedUnits.Add(clicked);
            }
        }
        else
        {
            // Single-select
            DeselectAll();
            clicked.SetSelected(true);
            selectedUnits.Add(clicked);
        }
    }

    private SelectableHighlight PickTopmostSelectable(Collider2D[] hits)
    {
        SelectableHighlight best = null;
        int bestSortingLayer = int.MinValue;
        int bestSortingOrder = int.MinValue;
        float bestZ = float.PositiveInfinity;

        foreach (var h in hits)
        {
            if (h == null) continue;

            var sel = h.GetComponent<SelectableHighlight>() ?? h.GetComponentInParent<SelectableHighlight>();
            if (sel == null) continue;

            var sr = sel.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                int layer = sr.sortingLayerID;
                int order = sr.sortingOrder;
                float z = sel.transform.position.z;

                bool better =
                    (layer > bestSortingLayer) ||
                    (layer == bestSortingLayer && order > bestSortingOrder) ||
                    (layer == bestSortingLayer && order == bestSortingOrder && z < bestZ);

                if (better)
                {
                    best = sel;
                    bestSortingLayer = layer;
                    bestSortingOrder = order;
                    bestZ = z;
                }
            }
            else
            {
                // Fallback bez SpriteRenderer: bliżej kamery (mniejsze Z)
                float z = sel.transform.position.z;
                bool better = best == null || z < bestZ;
                if (better)
                {
                    best = sel;
                    bestZ = z;
                }
            }
        }

        return best;
    }

    // ---------------- BOX SELECT ----------------

    private void SelectUnitsInBox()
    {
        if (selectionBox == null) return;

        Vector2 min = selectionBox.anchoredPosition;
        Vector2 max = min + selectionBox.sizeDelta;

        var allColliders = FindObjectsOfType<Collider2D>();
        foreach (var col in allColliders)
        {
            if (((1 << col.gameObject.layer) & selectableMask) == 0) continue;

            var sel = col.GetComponent<SelectableHighlight>() ?? col.GetComponentInParent<SelectableHighlight>();
            if (sel == null) continue;

            Vector3 sp = mainCam.WorldToScreenPoint(sel.transform.position);
            if (sp.x >= min.x && sp.x <= max.x && sp.y >= min.y && sp.y <= max.y)
            {
                sel.SetSelected(true);
                if (!selectedUnits.Contains(sel))
                    selectedUnits.Add(sel);
            }
        }
    }

    // ---------------- MISC ----------------

    private void DeselectAll()
    {
        for (int i = selectedUnits.Count - 1; i >= 0; i--)
        {
            if (selectedUnits[i] == null) selectedUnits.RemoveAt(i);
        }

        for (int i = 0; i < selectedUnits.Count; i++)
            selectedUnits[i].SetSelected(false);

        selectedUnits.Clear();
    }

    private void UpdateBox(Vector2 currentMouse)
    {
        Vector2 start = startPos;
        Vector2 size = currentMouse - start;

        if (size.x < 0) { start.x += size.x; size.x = -size.x; }
        if (size.y < 0) { start.y += size.y; size.y = -size.y; }

        selectionBox.anchoredPosition = start;
        selectionBox.sizeDelta = size;
    }

    public List<SelectableHighlight> GetSelectedUnits() => selectedUnits;
}
