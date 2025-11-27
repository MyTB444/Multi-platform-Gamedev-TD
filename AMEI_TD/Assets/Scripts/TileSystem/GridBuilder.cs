using System.Collections.Generic;
using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    [SerializeField] private GameObject mainPrefab;
    [SerializeField] private int gridLength = 10;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private float tileSpacing = 2f;
    [SerializeField] private List<GameObject> createdTiles;

    public List<GameObject> GetTileSetup() => createdTiles;

    [ContextMenu("Build Grid")]
    private void BuildGrid()
    {
        ClearGrid();
        createdTiles = new List<GameObject>();
        
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridWidth; z++)
            {
                CreateTile(x, z);
            }
        }
    }

    [ContextMenu("Clear Grid")]
    private void ClearGrid()
    {
        foreach (GameObject tile in createdTiles)
        {
            DestroyImmediate(tile);
        }
        
        createdTiles.Clear();
    }
    
    private void CreateTile(float xPosition, float zPosition)
    {
        Vector3 newPosition = new Vector3(xPosition * tileSpacing, 0, zPosition * tileSpacing);
        GameObject newTile = Instantiate(mainPrefab, newPosition, Quaternion.identity, transform);
        
        createdTiles.Add(newTile);
    }
}