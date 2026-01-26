using UnityEngine;

[CreateAssetMenu(menuName = "ToysRevolution/Inventory/Item Config", fileName = "Item_")]
public class ItemConfig : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Behavior")]
    public ItemType itemType = ItemType.Generic;

    [Tooltip("Np. Remains - może się stackować w tym samym slocie.")]
    public bool stackable = false;

    // Opcjonalne parametry (na później)
    public int intValue = 0;       // np. heal amount / key color id / itp.
    public string stringValue = ""; // np. nazwa koloru klucza
}

public enum ItemType
{
    Generic,
    Key,
    Float,
    Remains,
    Heal,
    Tool
}
