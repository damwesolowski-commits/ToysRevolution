using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GridData", menuName = "Game/Grid Data")]
public class GridData : ScriptableObject, ISerializationCallbackReceiver
{
    [System.Serializable]
    public class CellData
    {
        public Vector2Int position;
        public bool walkable;
        public bool isObstacleHard;
        public bool isObstacleSoft;
        public bool isDeadly;
        public bool isSlippery;
        public bool isBridge;
        public bool isSwitch;
        public bool isSpecial;
        public bool isArrow;
        public bool isSpike;
        public Vector2Int arrowDirection;
        public float cost = 1f;
        public ButtonBase button;
        public bool hasButton { get; set; }
    }

    public Vector2Int size;
    public Dictionary<Vector2Int, CellData> cells = new();

    // 🔹 Listy pomocnicze do serializacji
    [SerializeField] private List<Vector2Int> serializedKeys = new();
    [SerializeField] private List<CellData> serializedValues = new();

    // 🔹 Dodawanie lub aktualizacja komórki
    public void SetCell(Vector2Int pos, CellData data)
    {
        data.position = pos;
        cells[pos] = data;
    }

    // 🔹 Pobieranie komórki
    public CellData GetCell(Vector2Int pos)
    {
        cells.TryGetValue(pos, out var cell);
        return cell;
    }

    // 🔹 Zliczanie komórek
    public int Count => cells?.Count ?? 0;

    // 🔹 Serializacja (z Dictionary → Listy)
    public void OnBeforeSerialize()
    {
        serializedKeys.Clear();
        serializedValues.Clear();

        foreach (var kvp in cells)
        {
            serializedKeys.Add(kvp.Key);
            serializedValues.Add(kvp.Value);
        }
    }

    // 🔹 Deserializacja (z List → Dictionary)
    public void OnAfterDeserialize()
    {
        cells = new Dictionary<Vector2Int, CellData>();

        for (int i = 0; i < serializedKeys.Count; i++)
        {
            if (i < serializedValues.Count)
                cells[serializedKeys[i]] = serializedValues[i];
        }
    }
}
