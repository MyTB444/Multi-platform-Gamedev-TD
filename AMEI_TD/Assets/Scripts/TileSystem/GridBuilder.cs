using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor tool for creating a grid of tiles. Use the context menu (right-click component)
/// to build or clear the grid. Tiles are instantiated as children of this GameObject.
/// </summary>
public class GridBuilder : MonoBehaviour
{
    [SerializeField] private GameObject mainPrefab;       // The tile prefab to instantiate
    [SerializeField] private int gridLength = 10;         // Number of tiles along X axis
    [SerializeField] private int gridWidth = 10;          // Number of tiles along Z axis
    [SerializeField] private float tileSpacing = 2f;      // World units between tile centers
    [SerializeField] private List<GameObject> createdTiles;  // References to all instantiated tiles

    /// <summary>
    /// Returns the list of all tiles created by this grid builder.
    /// </summary>
    public List<GameObject> GetTileSetup() => createdTiles;

    /// <summary>
    /// Creates a new grid of tiles. Clears any existing grid first.
    /// Access via right-click → "Build Grid" in the Inspector.
    /// </summary>
    [ContextMenu("Build Grid")]
    private void BuildGrid()
    {
        ClearGrid();
        createdTiles = new List<GameObject>();
        
        // Nested loop creates gridLength × gridWidth tiles
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridWidth; z++)
            {
                CreateTile(x, z);
            }
        }
    }

    /// <summary>
    /// Destroys all tiles in the grid.
    /// Access via right-click → "Clear Grid" in the Inspector.
    /// </summary>
    [ContextMenu("Clear Grid")]
    private void ClearGrid()
    {
        foreach (GameObject tile in createdTiles)
        {
            DestroyImmediate(tile);
        }
        
        createdTiles.Clear();
    }
    
    /// <summary>
    /// Instantiates a single tile at the specified grid coordinates.
    /// Position is calculated by multiplying grid coords by tileSpacing.
    /// </summary>
    /// <param name="xPosition">Grid X coordinate</param>
    /// <param name="zPosition">Grid Z coordinate</param>
    private void CreateTile(float xPosition, float zPosition)
    {
        Vector3 newPosition = new Vector3(xPosition * tileSpacing, 0, zPosition * tileSpacing);
        GameObject newTile = Instantiate(mainPrefab, newPosition, Quaternion.identity, transform);
        
        createdTiles.Add(newTile);
    }
}