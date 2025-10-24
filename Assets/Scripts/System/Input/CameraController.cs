using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float scrollSpeed = 5f;          // prędkość przesuwania kamery
    public float edgeSize = 10f;            // szerokość strefy przy krawędziach ekranu (w pikselach)
    public Vector2 minPosition;             // minimalne granice ruchu kamery
    public Vector2 maxPosition;             // maksymalne granice ruchu kamery

    void Update()
    {
        Vector3 pos = transform.position;

        // Odczytaj pozycję myszy
        Vector3 mousePos = Input.mousePosition;

        // Ruch poziomy
        if (mousePos.x <= edgeSize)
            pos.x -= scrollSpeed * Time.deltaTime;
        else if (mousePos.x >= Screen.width - edgeSize)
            pos.x += scrollSpeed * Time.deltaTime;

        // Ruch pionowy
        if (mousePos.y <= edgeSize)
            pos.y -= scrollSpeed * Time.deltaTime;
        else if (mousePos.y >= Screen.height - edgeSize)
            pos.y += scrollSpeed * Time.deltaTime;

        // Ogranicz kamerę do granic mapy
        pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
        pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);

        transform.position = pos;
    }
}
