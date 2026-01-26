using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class SecretTileData
{
    [Header("Tilemap i pozycja")]
    public Tilemap targetTilemap;      // Na której Tilemapie dokonujemy zmiany
    public Vector3Int position;        // Pozycja kafla (x, y, 0)

    [Header("Zachowanie kafla")]
    public TileBase tileToPlace;       // Jaki Rule Tile ma się pojawić
    public bool removeOnActivate = false; // Czy kafel ma zniknąć po aktywacji

    [Header("Czas aktywacji")]
    public float startDelay = 0f;      // Opóźnienie przed aktywacją
    public float effectDuration = 5f;  // Jak długo kafel ma być aktywny
}
