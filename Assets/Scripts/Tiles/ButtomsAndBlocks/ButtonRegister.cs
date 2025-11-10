using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ButtonBase))]
public class ButtonRegister : MonoBehaviour
{
    private IEnumerator Start()
    {
        // ⏳ Poczekaj 1 klatkę, żeby TileLogicManager i GridData były gotowe
        yield return null;

        var manager = TileLogicManager.Instance;
        if (manager == null || manager.gridData == null)
        {
            Debug.LogWarning($"[{name}] ❌ Nie można zarejestrować przycisku – brak TileLogicManager lub GridData!");
            yield break;
        }

        // 🧭 Wylicz pozycję kafelka
        Vector3 worldPos = transform.position;
        Vector3Int cellPos = manager.groundTilemap.WorldToCell(worldPos);
        Vector2Int gridPos = new Vector2Int(cellPos.x, cellPos.y);

        // 🧩 Pobierz lub utwórz dane komórki
        var cellData = manager.gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData();
            manager.gridData.SetCell(gridPos, cellData);
        }

        // ✅ Zarejestruj przycisk w komórce
        cellData.hasButton = true;
        cellData.button = GetComponent<ButtonBase>();
        manager.gridData.SetCell(gridPos, cellData);

        Debug.Log($"✅ Zarejestrowano przycisk {name} w komórce {gridPos}");
    }
}
