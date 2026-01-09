using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Represents a single tile in the grid. Handles tile swapping, prop placement, and collider updates.
/// </summary>
public class TileSlot : MonoBehaviour
{
    // Cached original state for potential restoration
    private int originalLayerIndex;
    private Material originalMaterial;
    
    // ==================== COMPONENT ACCESSORS ====================
    // Properties that fetch components on demand (no caching)
    
    private MeshRenderer meshRenderer => GetComponent<MeshRenderer>();
    private MeshFilter meshFilter => GetComponent<MeshFilter>();
    private Collider myCollider => GetComponent<Collider>();
    private TileSetHolder tileSetHolder => GetComponentInParent<TileSetHolder>(true);

    private void Awake()
    {
        // Cache original state on startup
        originalLayerIndex = gameObject.layer;
        originalMaterial = GetComponent<MeshRenderer>().sharedMaterial;
    }
    
    /// <summary>
    /// Replaces this tile's appearance with another tile's mesh, material, collider, and children.
    /// Used by the level editor to swap tile types without destroying/recreating GameObjects.
    /// </summary>
    /// <param name="referenceTile">The tile prefab to copy appearance from</param>
    public void SwitchTile(GameObject referenceTile)
    {
        // Update name to match the new tile type
        gameObject.name = referenceTile.name;

        TileSlot newTile = referenceTile.GetComponent<TileSlot>();

        // Copy visual components
        meshFilter.mesh = newTile.GetMesh();
        meshRenderer.material = newTile.GetMaterial();

        // Copy physics and hierarchy
        UpdateCollider(newTile.GetCollider());
        UpdateChildren(newTile);
        UpdateLayer(referenceTile);
        UpdateLocalRotation(referenceTile);
        UpdateLocalScale(referenceTile);
    }

    #region Prop Management
    
    /// <summary>
    /// Adds a prop to this tile. Multiple props can be stacked.
    /// Automatically calculates Y position so prop sits on top of tile surface.
    /// </summary>
    /// <param name="propPrefab">The prop prefab to instantiate</param>
    public void AddProp(GameObject propPrefab)
    {
        if (propPrefab == null) return;
        
        // ===== CALCULATE TILE TOP =====
        // Get tile's mesh bounds (local space) - we need the top of the tile
        float tileTopY = 0f;
        MeshFilter tileMF = GetComponent<MeshFilter>();
        if (tileMF != null && tileMF.sharedMesh != null)
        {
            Bounds tileBounds = tileMF.sharedMesh.bounds;
            // bounds.max.y gives us the top of the mesh in local space
            // Multiply by scale to get actual world-relative height
            tileTopY = tileBounds.max.y * transform.localScale.y;
        }
        
        // ===== CALCULATE PROP BOTTOM OFFSET =====
        // Get prop's mesh bounds from the PREFAB (before instantiating)
        // We need to know how far the bottom of the prop is from its pivot (center)
        float propBottomOffset = 0f;
        MeshFilter propMF = propPrefab.GetComponent<MeshFilter>();
        if (propMF == null)
        {
            // Try children if not on root
            propMF = propPrefab.GetComponentInChildren<MeshFilter>();
        }
        if (propMF != null && propMF.sharedMesh != null)
        {
            Bounds propBounds = propMF.sharedMesh.bounds;
            // bounds.min.y is negative if pivot is at center (bottom is below pivot)
            // We need to offset by the negative of this to bring the bottom up to y=0
            propBottomOffset = -propBounds.min.y * propPrefab.transform.localScale.y;
        }
        
        // ===== INSTANTIATE AND POSITION =====
        GameObject newProp = Instantiate(propPrefab, transform);
        newProp.name = propPrefab.name;
        
        // Final Y = tile top + prop bottom offset (to sit prop's bottom on tile's top)
        float finalY = tileTopY + propBottomOffset;
        
        newProp.transform.localPosition = new Vector3(0, finalY, 0);
        newProp.transform.localRotation = Quaternion.identity;
    }
    
    /// <summary>
    /// Removes all props from this tile.
    /// Iterates backwards to safely destroy while iterating.
    /// </summary>
    public void RemoveAllProps()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    
    /// <summary>
    /// Gets the count of child objects (props) on this tile.
    /// </summary>
    public int GetPropCount() => transform.childCount;
    
    #endregion

    /// <summary>
    /// Returns the material that was assigned when Awake() ran.
    /// Falls back to current material if original wasn't cached.
    /// </summary>
    public Material GetOriginalMaterial()
    {
        if (originalMaterial == null) originalMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        return originalMaterial;
    }

    public Material GetMaterial() => meshRenderer.sharedMaterial;
    public Mesh GetMesh() => meshFilter.sharedMesh;
    public Collider GetCollider() => myCollider;

    /// <summary>
    /// Replaces this tile's collider with a copy of the reference collider.
    /// Supports BoxCollider and MeshCollider types.
    /// </summary>
    /// <param name="newCollider">The collider to copy from</param>
    private void UpdateCollider(Collider newCollider)
    {
        // Remove existing collider
        DestroyImmediate(myCollider);

        // Copy BoxCollider properties
        if (newCollider is BoxCollider)
        {
            BoxCollider original = newCollider.GetComponent<BoxCollider>();
            BoxCollider myNewCollider = transform.AddComponent<BoxCollider>();

            myNewCollider.center = original.center;
            myNewCollider.size = original.size;
        }

        // Copy MeshCollider properties
        if (newCollider is MeshCollider)
        {
            MeshCollider original = newCollider.GetComponent<MeshCollider>();
            MeshCollider myNewCollider = transform.AddComponent<MeshCollider>();

            myNewCollider.sharedMesh = original.sharedMesh;
            myNewCollider.convex = original.convex;
        }
    }

    /// <summary>
    /// Replaces all child objects with copies from the reference tile.
    /// Used to copy decorations/sub-objects when switching tile types.
    /// </summary>
    /// <param name="newTile">The tile to copy children from</param>
    private void UpdateChildren(TileSlot newTile)
    {
        // Clear existing children (iterate backwards for safe removal)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Copy new children from reference tile
        foreach (Transform child in newTile.transform)
        {
            Instantiate(child.gameObject, transform);
        }
    }
    
    /// <summary>
    /// Copies local rotation from reference object.
    /// </summary>
    private void UpdateLocalRotation(GameObject referenceObj)
    {
        transform.localRotation = referenceObj.transform.localRotation;
    }

    /// <summary>
    /// Copies local scale from reference object.
    /// </summary>
    private void UpdateLocalScale(GameObject referenceObj)
    {
        transform.localScale = referenceObj.transform.localScale;
    }

    /// <summary>
    /// Updates this tile's layer to match the reference object.
    /// Also caches the new layer as the "original" layer.
    /// </summary>
    public void UpdateLayer(GameObject referenceObj)
    {
        gameObject.layer = referenceObj.layer;
        originalLayerIndex = gameObject.layer;
    }

    /// <summary>
    /// Rotates the tile by 90 degrees in the specified direction.
    /// </summary>
    /// <param name="dir">1 for clockwise, -1 for counter-clockwise</param>
    public void RotateTile(int dir)
    {
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.y += 90 * dir;
        transform.localEulerAngles = currentRotation;
    }

    /// <summary>
    /// Adjusts the tile's Y position by 0.1 units.
    /// </summary>
    /// <param name="verticalDir">1 for up, -1 for down</param>
    public void AdjustY(int verticalDir)
    {
        transform.position += new Vector3(0, 0.1f * verticalDir, 0);
    }
}