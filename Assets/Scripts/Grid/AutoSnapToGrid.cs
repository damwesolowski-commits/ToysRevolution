using UnityEngine;

[ExecuteInEditMode] // działa też w edytorze, nie tylko w Play Mode
public class AutoSnapToGrid : MonoBehaviour
{
    [Tooltip("Wielkość siatki (Grid Cell Size). Dla większości projektów to 1.")]
    public float gridSize = 1f;

    void Update()
    {
        // Działa tylko w edytorze (żeby nie marnować CPU w czasie gry)
        if (!Application.isPlaying)
        {
            Vector3 pos = transform.position;

            // Zaokrąglamy do środka kafelka (czyli np. 2.5, 3.5, ...)
            pos.x = Mathf.Floor(pos.x / gridSize) * gridSize + gridSize / 2f;
            pos.y = Mathf.Floor(pos.y / gridSize) * gridSize + gridSize / 2f;
            pos.z = 0; // utrzymujemy obiekt w 2D

            transform.position = pos;
        }
    }
}
