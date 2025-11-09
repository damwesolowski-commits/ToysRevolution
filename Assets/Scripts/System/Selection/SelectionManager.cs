using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform selectionBox;

    [Header("Hit-detection")]
    [Tooltip("Warstwy z jednostkami (np. Units albo Player).")]
    [SerializeField] private LayerMask selectableMask;

    [Header("Debug")]
    public bool logDebug = false;

    [Header("Tilemap Reference")]
    [SerializeField] private Tilemap groundTilemap;

    private Vector2 startPos;
    private Camera mainCam;
    private readonly List<SelectableHighlight> selectedUnits = new List<SelectableHighlight>();
    private bool isDragging = false;
    private const float dragThreshold = 15f;
    private Vector2 pressScreen;     // surowy punkt kliknięcia (ekran)
    private Vector2 anchorScreen;    // środek kafelka (ekran) – od tego rysujemy box
    private Vector3Int dragStartCell; // komórka kafelka, na której zaczęto przeciąganie
    private Vector3 anchorWorld; // pozycja w świecie, gdzie rozpoczęto drag
    private bool suppressClickUp; // tłumi HandleClickSelect po drag

    void Start()
    {
        Instance = this;
        mainCam = Camera.main;

        if (selectionBox != null)
            selectionBox.gameObject.SetActive(false);

        if (selectableMask.value == 0)
            selectableMask = LayerMask.GetMask("Player");
    }

    void Update()
    {
        // --- Kliknięcie LPM (ustawienie punktów startowych) ---
        if (Input.GetMouseButtonDown(0))
        {
            pressScreen = Input.mousePosition;          // do progu przeciągania

            // wylicz środek kafelka jako kotwicę boxa
            Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            dragStartCell = groundTilemap.WorldToCell(mouseWorld);

            Vector3 cellCenter = groundTilemap.GetCellCenterWorld(dragStartCell);
            anchorWorld = cellCenter; // zapamiętaj pozycję w świecie
            anchorScreen = mainCam.WorldToScreenPoint(anchorWorld); // nadal obliczamy początkową pozycję, żeby box pojawił się od razu

            isDragging = false;
        }

        // --- Przytrzymanie LPM (aktywacja boxa po przekroczeniu progu) ---
        if (Input.GetMouseButton(0))
        {
            float distance = Vector2.Distance(pressScreen, (Vector2)Input.mousePosition);

            if (distance > dragThreshold)
            {
                if (!isDragging)
                {
                    isDragging = true;
                    suppressClickUp = true;

                    if (selectionBox != null)
                    {
                        selectionBox.gameObject.SetActive(true);
                        UpdateBoxFromAnchor(Input.mousePosition); // używa anchorScreen
                    }
                }

                if (isDragging && selectionBox != null)
                    UpdateBoxFromAnchor(Input.mousePosition);
            }
        }

        // --- Puszczenie LPM ---
        if (Input.GetMouseButtonUp(0))
        {
            // 1) Jeśli był drag w tym cyklu — zakończ selekcję prostokątem
            if (isDragging)
            {
                if (selectionBox != null)
                    selectionBox.gameObject.SetActive(false);

                Vector3 mouseWorldUp = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldUp.z = 0f;
                Vector3Int dragEndCell = groundTilemap.WorldToCell(mouseWorldUp);

                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!shift)
                    DeselectAll(); // CZYSZCZENIE TYLKO PRZED selekcją boxem

                SelectUnitsInCellsRect(dragStartCell, dragEndCell);
                // opcjonalnie „pewniak”:
                // ForceIncludeStartCellUnit();

                // zresetuj stan i WYJDŹ (nie pozwól wejść w ścieżkę kliknięcia)
                isDragging = false;
                suppressClickUp = false;
                return; // <<— KLUCZOWE: kończymy tu MouseUp po drag!
            }

            // 2) Jeśli NIE było dragu, ale poprzednio go rozpoczęliśmy — stłum klik
            if (suppressClickUp)
            {
                // safety: nie wykonuj HandleClickSelect w tym frame
                suppressClickUp = false;
                isDragging = false;
                return;
            }

            // 3) Normalny pojedynczy klik (bez drag)
            HandleClickSelect();
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
   

        // 🔹 Szukamy najwyżej położonego SelectableHighlight
        SelectableHighlight clicked = PickTopmostSelectable(hits);

        // 🔹 Jeżeli żaden SelectableHighlight nie znaleziony — NIE czyść selekcji
        if (clicked == null)
        {
           // if (logDebug) Debug.Log("Selection: no selectable under cursor (keeping current selection)");
            return; // klik w puste pole nic nie robi — zostaje poprzednia selekcja
        }

        // 🔹 Wypisanie debug hitów
        //if (logDebug)
        //{
        //    foreach (var h in hits)
        //        Debug.Log($"Selection hit: {h.name} (layer={LayerMask.LayerToName(h.gameObject.layer)})");
        //}

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
    private void SelectUnitsInCellsRect(Vector3Int a, Vector3Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        Vector2 cellSize = groundTilemap.layoutGrid.cellSize;

        // Centra skrajnych komórek
        Vector3 minCellCenter = groundTilemap.GetCellCenterWorld(new Vector3Int(minX, minY, 0));
        Vector3 maxCellCenter = groundTilemap.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0));

        // Dokładne rogi świata tych komórek
        Vector3 minWorld = new Vector3(minCellCenter.x - cellSize.x * 0.5f,
                                       minCellCenter.y - cellSize.y * 0.5f, 0f);
        Vector3 maxWorld = new Vector3(maxCellCenter.x + cellSize.x * 0.5f,
                                       maxCellCenter.y + cellSize.y * 0.5f, 0f);

        // Lekko POSZERZAMY, nie zwężamy: epsilon chroni skraje
        const float epsilon = 0.01f;
        Vector3 centerWorld = (minWorld + maxWorld) * 0.5f;
        Vector2 boxSize = new Vector2((maxWorld.x - minWorld.x) + epsilon,
                                      (maxWorld.y - minWorld.y) + epsilon);

        Collider2D[] hits = Physics2D.OverlapBoxAll(centerWorld, boxSize, 0f, selectableMask);
        if (hits == null || hits.Length == 0) return;

        foreach (var h in hits)
        {
            var sel = h.GetComponent<SelectableHighlight>() ?? h.GetComponentInParent<SelectableHighlight>();
            if (sel == null) continue;

            if (!selectedUnits.Contains(sel))
            {
                sel.SetSelected(true);
                selectedUnits.Add(sel);
            }
        }
    }


    private void ForceIncludeStartCellUnit()
    {
        // wylicz granice komórki w świecie
        Vector3 cellCenter = groundTilemap.GetCellCenterWorld(dragStartCell);
        Vector3 cellSize = (Vector3)groundTilemap.layoutGrid.cellSize;

        // niewielki margines, by złapać collidery mniejsze/większe niż tile
        Vector2 boxSize = new Vector2(cellSize.x * 0.95f, cellSize.y * 0.95f);

        // pobierz wszystkie collidery jednostek w tej komórce
        Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, boxSize, 0f, selectableMask);
        if (hits == null || hits.Length == 0) return;

        foreach (var h in hits)
        {
            var sel = h.GetComponent<SelectableHighlight>() ?? h.GetComponentInParent<SelectableHighlight>();
            if (sel == null) continue;

            // jeśli nie jest już na liście – dodaj
            if (!selectedUnits.Contains(sel))
            {
                sel.SetSelected(true);
                selectedUnits.Add(sel);
            }
        }
    }

    // ---------------- MISC ----------------

    private void DeselectAll()
    {
        foreach (var unit in selectedUnits)
        {
            if (unit != null)
                unit.SetSelected(false);
        }
        selectedUnits.Clear();
    }

    private void UpdateBoxFromAnchor(Vector2 currentMouse)
    {
        // 🔹 Przelicz anchor ze świata na ekran, żeby pozostał w miejscu mimo ruchu kamery
        Vector2 anchorNow = mainCam.WorldToScreenPoint(anchorWorld);

        Vector2 start = anchorNow;
        Vector2 size = currentMouse - start;

        if (size.x < 0) { start.x += size.x; size.x = -size.x; }
        if (size.y < 0) { start.y += size.y; size.y = -size.y; }

        selectionBox.anchoredPosition = start;
        selectionBox.sizeDelta = size;
    }

    public List<SelectableHighlight> GetSelectedUnits() => selectedUnits;

    public bool HasSelectedUnits()
    {
        return selectedUnits.Count > 0;
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}
