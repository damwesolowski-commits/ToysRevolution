using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class TileCursorFollow : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private float zOffset = -0.01f; // żeby marker był NAD tilemapą

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 1) AUTO-DOPASOWANIE ROZMIARU SPRITE’A DO ROZMIARU KAFLA
        // (działa niezależnie od Pixels Per Unit i skal rodziców)
        Vector2 cell = groundTilemap.layoutGrid.cellSize;
        Vector2 spriteSize = sr.sprite.bounds.size;
        Vector3 scale = transform.localScale;

        // zabezpieczenie przed dzieleniem przez zero
        if (spriteSize.x > 0.0001f && spriteSize.y > 0.0001f)
        {
            scale.x *= cell.x / spriteSize.x;
            scale.y *= cell.y / spriteSize.y;
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        // 2) RZUTOWANIE PROMIENIEM NA PŁASZCZYZNĘ TILEMAPY (zero problemów z Z)
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreen);

        // Płaszczyzna XY przechodząca przez Z tilemapy
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, groundTilemap.transform.position.z));

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);

            // Świat -> Komórka siatki
            Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);

            // Komórka -> środek w świecie
            Vector3 cellCenter = groundTilemap.GetCellCenterWorld(cellPos);

            // Ustaw marker; minimalny offset Z, aby był nad kaflami
            transform.position = new Vector3(cellCenter.x, cellCenter.y,
                                             groundTilemap.transform.position.z + zOffset);
        }
    }
}
