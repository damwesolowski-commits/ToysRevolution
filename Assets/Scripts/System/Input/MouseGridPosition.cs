using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class MouseGridPosition : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap groundTilemap;

    public Vector3 WorldPos { get; private set; }
    public Vector3Int CellPos { get; private set; }

    void Update()
    {
        // 1) Ekran → (x,y)
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        // 2) Oblicz właściwy dystans Z od kamery do płaszczyzny tilemapy
        // (w 2D zwykle kamera: -10, tilemap: 0 → dystans = 10)
        float zDistance = Mathf.Abs(groundTilemap.transform.position.z - mainCamera.transform.position.z);

        // 3) Ekran → Świat z poprawnym Z
        WorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDistance));

        // 4) Świat → Komórka siatki
        CellPos = groundTilemap.WorldToCell(WorldPos);
        /*
        // Debug jednorazowo przy zmianie komórki
        if (!Application.isBatchMode) // by nie spamować w build-batch
            //Debug.Log($"Mouse over tile: {CellPos.x}, {CellPos.y}");
    }
        */
    }
}
