using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Collections;

public class TileCursorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private InputActionReference rightClickAction; // <Mouse>/rightButton
    [SerializeField] private GameObject pingRingPrefab; // Prefab efektu ping
    [SerializeField] private GameObject cursorPrefab; // ✅ Prefab kursora (np. TileCursorRing)

    [Header("Settings")]
    [SerializeField] private float zOffset = -0.01f;
    [SerializeField] private bool useRawMouseFallback = true; // nasłuchuje PPM bez Input Actions

    private InputAction _clickAction;
    private GameObject _cursorInstance;
    private Vector3 _pingBaseScale = Vector3.one; // skala dopasowana do kafla

    private void OnEnable()
    {
        if (rightClickAction != null)
        {
            _clickAction = rightClickAction.action;
            _clickAction.started += OnPointerDown;
            _clickAction.canceled += OnPointerUp;
            _clickAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (_clickAction != null)
        {
            _clickAction.started -= OnPointerDown;
            _clickAction.canceled -= OnPointerUp;
            _clickAction.Disable();
        }
    }

    private void Start()
    {
        // ✅ Tworzymy instancję kursora prefab
        if (cursorPrefab != null && groundTilemap != null)
        {
            _cursorInstance = Instantiate(cursorPrefab, transform.position, Quaternion.identity);
            _cursorInstance.transform.SetParent(transform); // porusza się razem z obiektem TileCursor

            // dopasowanie rozmiaru do kafla
            var sr = _cursorInstance.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector2 cell = groundTilemap.layoutGrid.cellSize;
                Vector2 spriteSize = sr.sprite.bounds.size;
                if (spriteSize.x > 0.0001f && spriteSize.y > 0.0001f)
                {
                    _cursorInstance.transform.localScale = new Vector3(
                        cell.x / spriteSize.x,
                        cell.y / spriteSize.y,
                        1f
                    );
                }
            }
        }

        // dopasowanie pingu do kafla
        if (pingRingPrefab != null && groundTilemap != null)
        {
            var psr = pingRingPrefab.GetComponent<SpriteRenderer>();
            if (psr != null && psr.sprite != null)
            {
                Vector2 cell = groundTilemap.layoutGrid.cellSize;
                Vector2 pingSize = psr.sprite.bounds.size;
                if (pingSize.x > 0.0001f && pingSize.y > 0.0001f)
                {
                    _pingBaseScale = new Vector3(
                        cell.x / pingSize.x,
                        cell.y / pingSize.y,
                        1f
                    );
                }
            }
        }
    }

    private void Update()
    {
        // Ukrywanie kursora podczas przeciągania zaznaczenia
        if (SelectionManager.Instance != null && SelectionManager.Instance.IsDragging())
        {
            if (_cursorInstance != null) _cursorInstance.SetActive(false);
            return;
        }
        else if (_cursorInstance != null && !_cursorInstance.activeSelf)
        {
            _cursorInstance.SetActive(true);
        }

        // ruch kursora po siatce
        if (mainCamera != null && groundTilemap != null)
        {
            Vector2 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Ray ray = mainCamera.ScreenPointToRay(mouseScreen);
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, groundTilemap.transform.position.z));

            if (plane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);
                Vector3 cellCenter = groundTilemap.GetCellCenterWorld(cellPos);

                transform.position = new Vector3(
                    cellCenter.x, cellCenter.y,
                    groundTilemap.transform.position.z + zOffset
                );
            }
        }

        // fallback na czysty PPM
        if (useRawMouseFallback && Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame) OnPointerDown(default);
            if (Mouse.current.rightButton.wasReleasedThisFrame) OnPointerUp(default);
        }
    }

    private void OnPointerDown(InputAction.CallbackContext ctx)
    {
        if (_cursorInstance != null)
            _cursorInstance.SetActive(false);
    }

    private void OnPointerUp(InputAction.CallbackContext ctx)
    {
        if (_cursorInstance != null)
            _cursorInstance.SetActive(true);

        if (SelectionManager.Instance != null && SelectionManager.Instance.HasSelectedUnits())
        {
            SpawnPingEffect();
        }
    }

    private void SpawnPingEffect()
    {
        if (pingRingPrefab == null) return;

        GameObject ping = Instantiate(pingRingPrefab, transform.position, Quaternion.identity);
        ping.transform.localScale = _pingBaseScale;

        StartCoroutine(PingPulse(ping));
    }

    private IEnumerator PingPulse(GameObject ping)
    {
        SpriteRenderer pingSR = ping.GetComponent<SpriteRenderer>();
        float duration = 0.3f;
        float elapsed = 0f;

        const float startFactor = 1.0f;
        const float endFactor = 0.2f;

        Color baseColor = pingSR != null ? pingSR.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float factor = Mathf.Lerp(startFactor, endFactor, Mathf.SmoothStep(0f, 1f, t));
            ping.transform.localScale = _pingBaseScale * factor;

            if (pingSR != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                pingSR.color = c;
            }

            yield return null;
        }

        Destroy(ping);
    }
}
