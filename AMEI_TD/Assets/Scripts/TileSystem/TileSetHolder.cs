using System.Collections.Generic;
using UnityEngine;

public class TileSetHolder : MonoBehaviour
{
    public enum TileTheme { Field, Snow, Desert, Dungeon }
    
    [Header("Theme")]
    public TileTheme theme = TileTheme.Field;
    
    [Header("Field Tiles (Checkered)")]
    public GameObject tileFieldLight;
    public GameObject tileFieldDark;

    [Header("Roads")]
    public GameObject tileRoad;

    [Header("Corners")]
    public GameObject tileInnerCorner;
    public GameObject tileInnerCorner2;
    public GameObject tileOuterCorner;
    public GameObject tileOuterCorner2;

    [Header("Sideways")]
    public GameObject tileSidewayRoad;
    public GameObject tileSidewayRoad2;

    [Header("Water")]
    public GameObject tileRiver;
    public GameObject tilePond;
    public GameObject tileRiverBridge;
    public GameObject tileRiverDoubleBridge;

    [Header("Props")]
    public List<PropCategory> propCategories = new List<PropCategory>();
}

[System.Serializable]
public class PropCategory
{
    public string categoryName = "New Category";
    public List<PropEntry> props = new List<PropEntry>();
}

[System.Serializable]
public class PropEntry
{
    public string displayName = "New Prop";
    public GameObject prefab;
}