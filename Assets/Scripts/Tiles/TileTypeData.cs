using UnityEngine;

[System.Serializable]
public class TileTypeData
{
    public string name;
    public bool walkable = true;
    public bool deadly = false;
    public bool requiresFloatItem = false;

    public TileTypeData(string name, bool walkable, bool deadly, bool requiresFloatItem)
    {
        this.name = name;
        this.walkable = walkable;
        this.deadly = deadly;
        this.requiresFloatItem = requiresFloatItem;
    }
}
