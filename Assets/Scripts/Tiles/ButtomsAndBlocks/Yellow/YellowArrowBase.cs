using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public abstract class YellowArrowBase : MonoBehaviour
{
    [Header("Group Settings")]
    public int groupId;

    [Header("Sound")]
    public AudioClip switchSound;

    protected SpriteRenderer spriteRenderer;
    protected AudioSource audioSource;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // 🔹 Zmiana kierunku (wywoływana przez przyciski)
    public void ToggleDirection()
    {
        bool changed = OnToggleDirection();
        if (changed)
        {
            UpdateGridArrowDirection(); // 🧩 Aktualizuj kierunek w GridData
            if (switchSound != null)
                audioSource.PlayOneShot(switchSound);
        }
    }

    // 🔹 Implementacja w klasach potomnych
    protected abstract bool OnToggleDirection();

    // ======================================================
    // 🧭 Aktualizacja kierunku w GridData
    // ======================================================
    protected void UpdateGridArrowDirection()
    {
        var manager = TileLogicManager.Instance;
        if (manager == null || manager.gridData == null) return;

        Vector3Int cellPos = manager.groundTilemap.WorldToCell(transform.position);
        Vector2Int gridPos = new Vector2Int(cellPos.x, cellPos.y);

        var cellData = manager.gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData();
            manager.gridData.SetCell(gridPos, cellData);
        }

        cellData.isArrow = true;
        cellData.walkable = true;
        cellData.isObstacleHard = false;

        // 🟨 Ustal kierunek w zależności od typu strzałki
        if (this is YellowArrowUpDown upDown)
        {
            var field = typeof(YellowArrowUpDown).GetField("isPointingUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isUp = (bool)field.GetValue(upDown);
            cellData.arrowDirection = isUp ? Vector2Int.up : Vector2Int.down;
        }
        else if (this is YellowArrowRightLeft rightLeft)
        {
            var field = typeof(YellowArrowRightLeft).GetField("isPointingRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isRight = (bool)field.GetValue(rightLeft);
            cellData.arrowDirection = isRight ? Vector2Int.right : Vector2Int.left;
        }

        manager.gridData.SetCell(gridPos, cellData);
        Debug.Log($"🔄 Zaktualizowano kierunek strzałki {name} → {cellData.arrowDirection}");
    }
}
