using System;                  // NOWE
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Ruch kamery")]
    public float scrollSpeed = 5f;
    public float edgeSize = 10f;              // szerokość „strefy przesuwania” przy krawędziach ekranu
    public Vector2 minPosition;               // lewy-dolny róg mapy (w world space)
    public Vector2 maxPosition;               // prawy-górny róg mapy (w world space)

    [Header("Zoom (scroll myszki)")]
    public float zoomSpeed = 5f;              // jak szybko zmienia się docelowy zoom
    public float minZoom = 3f;                // najmocniejsze przybliżenie (mniejsza wartość = bliżej)
    public float maxZoom = 8f;                // największe oddalenie

    [Header("Płynny zoom")]
    public float zoomLerpSpeed = 10f;         // jak szybko kamera „dojeżdża” do celu

    [Header("Ustawienia startowe")]
    public float startZoom = 5f;              // początkowy zoom (orthographicSize na starcie)

    [Header("Zoom do kursora (RTS)")]
    public bool zoomToMouse = true;           // czy zoom ma przyciągać kamerę do kursora

    [Header("Zoom klawiszami + / -")]
    public KeyCode zoomInKey = KeyCode.Equals;      // klawisz z „=” i „+” na głównej klawiaturze
    public KeyCode zoomOutKey = KeyCode.Minus;      // klawisz „-” na głównej klawiaturze
    public KeyCode zoomInKeyAlt = KeyCode.KeypadPlus;   // klawisz „+” na numpadzie
    public KeyCode zoomOutKeyAlt = KeyCode.KeypadMinus; // klawisz „-” na numpadzie
    public float keyZoomStep = 1f;             // jak mocno reagować na przytrzymanie klawisza

    [Header("Reset zoomu i fokus na Playerów")]
    public KeyCode resetZoomKey = KeyCode.Space; // klawisz resetu (domyślnie Spacja)
    public bool focusPlayersOnReset = true;      // czy przy resetcie ma centruować na Playerach
    public string playerTag = "Player";          // tag jednostek gracza, po których skaczemy

    private Camera cam;
    private float targetZoom;

    // --- DO RTS ZOOMA ---
    private bool zoomAnchorActive = false;          // czy aktualnie trzymamy „kotwicę” zoomu
    private Vector3 zoomAnchorWorld;                // punkt w świecie, który ma zostać pod kursorem
    private Vector3 zoomAnchorScreen;               // zamrożona pozycja kursora na ekranie w momencie zoomu

    // --- DO SKAKANIA PO PLAYERACH ---
    private int currentPlayerIndex = 0;             // który Player będzie następny przy resetcie

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam != null && cam.orthographic)
        {
            targetZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
            cam.orthographicSize = targetZoom;
        }
    }

    void Update()
    {
        if (cam == null || !cam.orthographic)
            return;

        // -----------------------
        //   RESET ZOOMU / SKOK DO PLAYERA (np. Spacja)
        // -----------------------
        if (Input.GetKeyDown(resetZoomKey))
        {
            // wyłączamy ewentualną kotwicę RTS-zoomu
            zoomAnchorActive = false;

            // wracamy do domyślnego zoomu (płynnie, bo targetZoom)
            targetZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);

            // ustawiamy kamerę na kolejnego Playera (jeśli są)
            FocusNextPlayer();
        }

        // -----------------------
        //   RUCH KAMERY
        // -----------------------

        Vector3 pos = transform.position;
        Vector3 mousePos = Input.mousePosition;

        // Prędkość przesuwania dopasowana do zoomu:
        // przy starZoom prędkość = scrollSpeed,
        // przy większym oddaleniu kamera jedzie szybciej w unitach,
        // przy większym przybliżeniu – wolniej.
        float zoomFactor = cam.orthographicSize / startZoom;
        float adjustedScrollSpeed = scrollSpeed * zoomFactor;

        // Ruch poziomy
        if (mousePos.x <= edgeSize)
            pos.x -= adjustedScrollSpeed * Time.deltaTime;
        else if (mousePos.x >= Screen.width - edgeSize)
            pos.x += adjustedScrollSpeed * Time.deltaTime;

        // Ruch pionowy
        if (mousePos.y <= edgeSize)
            pos.y -= adjustedScrollSpeed * Time.deltaTime;
        else if (mousePos.y >= Screen.height - edgeSize)
            pos.y += adjustedScrollSpeed * Time.deltaTime;

        transform.position = pos;

        // -----------------------
        //   ZOOM – SCROLL + KLAWISZE
        // -----------------------

        float zoomInput = Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(zoomInKey) || Input.GetKey(zoomInKeyAlt))
            zoomInput += keyZoomStep * Time.deltaTime;

        if (Input.GetKey(zoomOutKey) || Input.GetKey(zoomOutKeyAlt))
            zoomInput -= keyZoomStep * Time.deltaTime;

        if (Mathf.Abs(zoomInput) > 0.0001f)
        {
            // jeśli zaczynamy nową operację zoomu – ustaw kotwicę
            if (zoomToMouse && !zoomAnchorActive)
            {
                // zapamiętujemy ekranową pozycję kursora w momencie scrolla
                zoomAnchorScreen = Input.mousePosition;
                // przeliczamy ją na świat tylko raz
                zoomAnchorWorld = cam.ScreenToWorldPoint(zoomAnchorScreen);
                zoomAnchorActive = true;
            }

            targetZoom = Mathf.Clamp(
                targetZoom - zoomInput * zoomSpeed,
                minZoom,
                maxZoom
            );
        }

        // -----------------------
        //   PŁYNNE DOJAZD DO targetZoom (LERP)
        // -----------------------

        if (Mathf.Abs(cam.orthographicSize - targetZoom) > 0.001f)
        {
            float newSize = Mathf.Lerp(
                cam.orthographicSize,
                targetZoom,
                zoomLerpSpeed * Time.deltaTime
            );

            cam.orthographicSize = newSize;

            // RTS-zoom: przesuwamy kamerę tak,
            // aby punkt zoomAnchorWorld pozostał w tym samym miejscu na ekranie.
            if (zoomToMouse && zoomAnchorActive)
            {
                // używamy ZAMROŻONEJ pozycji kursora, nie aktualnej
                Vector3 worldAfter = cam.ScreenToWorldPoint(zoomAnchorScreen);
                Vector3 diff = zoomAnchorWorld - worldAfter;
                transform.position += diff;
            }
        }
        else
        {
            // zoom zakończony – wyłącz kotwicę
            zoomAnchorActive = false;
        }

        // -----------------------
        //   OGRANICZENIE DO OBSZARU MAPY (zależne od zoomu)
        // -----------------------
        ClampCameraPositionToMap();
    }

    /// <summary>
    /// Skacze do kolejnego Playera po tagu (Player1, Player2, ...), ustawiając go w centrum ekranu.
    /// Każde naciśnięcie resetZoomKey bierze następnego, w kółko.
    /// </summary>
    private void FocusNextPlayer()
    {
        if (!focusPlayersOnReset)
            return;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        if (players == null || players.Length == 0)
            return;

        // Uporsządkowujemy po nazwie, żeby kolejność była przewidywalna
        Array.Sort(players, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        if (currentPlayerIndex >= players.Length)
            currentPlayerIndex = 0;

        Transform target = players[currentPlayerIndex].transform;

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Length;

        // Przesuwamy kamerę tak, żeby Player był w centrum (Z zostaje ten sam)
        Vector3 pos = transform.position;
        pos.x = target.position.x;
        pos.y = target.position.y;
        transform.position = pos;

        // Od razu upewniamy się, że nie wyjechaliśmy poza mapę
        ClampCameraPositionToMap();
    }

    /// <summary>
    /// Ogranicza pozycję kamery tak, aby widoczny obszar nigdy nie wyszedł poza mapę.
    /// minPosition / maxPosition traktujemy jako LEWY-DOLNY i PRAWY-GÓRNY róg mapy.
    /// </summary>
    private void ClampCameraPositionToMap()
    {
        if (cam == null || !cam.orthographic)
            return;

        Vector3 pos = transform.position;

        float vertExtent = cam.orthographicSize;
        float horExtent = vertExtent * cam.aspect;

        float minX = minPosition.x + horExtent;
        float maxX = maxPosition.x - horExtent;
        float minY = minPosition.y + vertExtent;
        float maxY = maxPosition.y - vertExtent;

        if (minX > maxX)
            pos.x = (minPosition.x + maxPosition.x) * 0.5f;
        else
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

        if (minY > maxY)
            pos.y = (minPosition.y + maxPosition.y) * 0.5f;
        else
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}
