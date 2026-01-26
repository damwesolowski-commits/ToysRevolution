using UnityEngine;
using System.Collections;

/// <summary>
/// 🔹 Rejestruje prefabowe żółte strzałki w systemie GridData.
/// Dzięki temu TileLogicManager wykryje je tak samo jak zwykłe strzałki z Tilemap
/// i automatycznie wymusi ruch jednostki.
/// </summary>
[RequireComponent(typeof(YellowArrowBase))]
public class YellowArrowRegister : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Poczekaj jedną klatkę, aż TileLogicManager i GridData będą gotowe
        yield return null;

        var manager = TileLogicManager.Instance;
        if (manager == null || manager.gridData == null)
        {
            Debug.LogWarning($"[{name}] ❌ Nie można zarejestrować strzałki – brak TileLogicManager lub GridData!");
            yield break;
        }

        // Wylicz pozycję kafelka
        Vector3 worldPos = transform.position;
        Vector3Int cellPos = manager.groundTilemap.WorldToCell(worldPos);
        Vector2Int gridPos = new Vector2Int(cellPos.x, cellPos.y);

        // Pobierz lub utwórz dane komórki
        var cellData = manager.gridData.GetCell(gridPos);
        if (cellData == null)
        {
            cellData = new GridData.CellData();
            manager.gridData.SetCell(gridPos, cellData);
        }

        // Oznacz komórkę jako strzałkę
        cellData.isArrow = true;
        cellData.walkable = true;
        cellData.isObstacleHard = false;

        // Ustal kierunek na podstawie typu strzałki
        YellowArrowBase arrow = GetComponent<YellowArrowBase>();
        if (arrow is YellowArrowUpDown upDown)
        {
            // Zmienna prywatna – odczytujemy za pomocą Reflection
            var field = typeof(YellowArrowUpDown).GetField("isPointingUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isUp = (bool)field.GetValue(upDown);
            cellData.arrowDirection = isUp ? Vector2Int.up : Vector2Int.down;
        }
        else if (arrow is YellowArrowRightLeft rightLeft)
        {
            var field = typeof(YellowArrowRightLeft).GetField("isPointingRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isRight = (bool)field.GetValue(rightLeft);
            cellData.arrowDirection = isRight ? Vector2Int.right : Vector2Int.left;
        }

        // Zaktualizuj dane w gridzie
        manager.gridData.SetCell(gridPos, cellData);

        Debug.Log($"✅ Zarejestrowano strzałkę {name} w komórce {gridPos} z kierunkiem {cellData.arrowDirection}");
    }
}
