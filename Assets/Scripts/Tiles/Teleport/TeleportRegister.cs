using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TeleportBase))]
public class TeleportRegister : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;

        var manager = TileLogicManager.Instance;
        if (manager == null || manager.gridData == null)
        {
            Debug.LogWarning("⚠ Brak TileLogicManager! Teleport nie został zarejestrowany w gridData.");
            yield break;
        }

        // kafelek
        Vector3Int cell = manager.groundTilemap.WorldToCell(transform.position);
        Vector2Int gridPos = new Vector2Int(cell.x, cell.y);

        var cellData = manager.gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData();
            manager.gridData.SetCell(gridPos, cellData);
        }

        cellData.hasTeleport = true;
        cellData.teleport = GetComponent<TeleportBase>();

        Debug.Log($"✅ Zarejestrowano teleport w gridData w grupie {cellData.teleport.groupId}");
    }
}
