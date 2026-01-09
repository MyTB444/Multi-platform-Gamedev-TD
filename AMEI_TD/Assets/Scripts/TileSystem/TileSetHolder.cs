using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container for all tile prefabs and props used in a level's tileset.
/// Attach to the parent grid object. TileSlot components reference this
/// via GetComponentInParent to access available tiles for swapping.
/// 
/// Supports multiple visual themes (Field, Snow, Desert, Dungeon).
/// </summary>
public class TileSetHolder : MonoBehaviour
{
    /// <summary>
    /// Visual theme options for the tileset.
    /// </summary>
    public enum TileTheme { Field, Snow, Desert, Dungeon }
    
    [Header("Theme")]
    public TileTheme theme = TileTheme.Field;
    
    [Header("Road Y Offset")]
    [Tooltip("Y offset applied to roads, corners, and sideways tiles")]
    public float roadYOffset = 0f;
    
    // ==================== GROUND TILES ====================
    // Basic field tiles with checkerboard pattern support
    
    [Header("Field Tiles (Checkered)")]
    public GameObject tileFieldLight;   // Light colored ground tile
    public GameObject tileFieldDark;    // Dark colored ground tile

    // ==================== ROAD TILES ====================
    // Straight road sections
    
    [Header("Roads")]
    public GameObject tileRoad;         // Straight road segment

    // ==================== CORNER TILES ====================
    // Road corners for path bending
    
    [Header("Corners")]
    public GameObject tileInnerCorner;   // Inner corner variant 1
    public GameObject tileInnerCorner2;  // Inner corner variant 2
    public GameObject tileOuterCorner;   // Outer corner variant 1
    public GameObject tileOuterCorner2;  // Outer corner variant 2

    // ==================== SIDEWAYS TILES ====================
    // Road edges and side pieces
    
    [Header("Sideways")]
    public GameObject tileSidewayRoad;   // Side road variant 1
    public GameObject tileSidewayRoad2;  // Side road variant 2

    // ==================== WATER TILES ====================
    // Rivers, ponds, and bridge crossings
    
    [Header("Water")]
    public GameObject tileRiver;              // River segment
    public GameObject tilePond;               // Still water/pond
    public GameObject tileRiverBridge;        // Single bridge crossing
    public GameObject tileRiverDoubleBridge;  // Double bridge crossing

    // ==================== PROPS ====================
    // Decorative objects organized by category
    
    [Header("Props")]
    public List<PropCategory> propCategories = new List<PropCategory>();
}

/// <summary>
/// Groups related props together for easier organization in the editor.
/// Example categories: Trees, Rocks, Buildings, etc.
/// </summary>
[System.Serializable]
public class PropCategory
{
    public string categoryName = "New Category";    // Display name in editor
    public List<PropEntry> props = new List<PropEntry>();  // Props in this category
}

/// <summary>
/// A single prop entry with a display name and prefab reference.
/// </summary>
[System.Serializable]
public class PropEntry
{
    public string displayName = "New Prop";   // Friendly name shown in editor
    public GameObject prefab;                  // The prop prefab to instantiate
}