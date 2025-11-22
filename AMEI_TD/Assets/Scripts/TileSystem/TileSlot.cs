using Unity.VisualScripting;
using UnityEngine;

public class TileSlot : MonoBehaviour
{
    private int originalLayerIndex;
    private Material originalMaterial;
    
    private MeshRenderer meshRenderer => GetComponent<MeshRenderer>();
    private MeshFilter meshFilter => GetComponent<MeshFilter>();
    private Collider myCollider => GetComponent<Collider>();
    private TileSetHolder tileSetHolder => GetComponentInParent<TileSetHolder>(true);

    private void Awake()
    {
        originalLayerIndex = gameObject.layer;
        originalMaterial = GetComponent<MeshRenderer>().sharedMaterial;
    }
    
    public void SwitchTile(GameObject referenceTile)
    {
        gameObject.name = referenceTile.name;

        TileSlot newTile = referenceTile.GetComponent<TileSlot>();

        meshFilter.mesh = newTile.GetMesh();
        meshRenderer.material = newTile.GetMaterial();

        UpdateCollider(newTile.GetCollider());
        UpdateChildren(newTile);
        UpdateLayer(referenceTile);
        UpdateLocalRotation(referenceTile);
        UpdateLocalScale(referenceTile);
    }

    #region Prop Management
    
    /// <summary>
    /// Adds a prop to this tile. Multiple props can be stacked.
    /// </summary>
    public void AddProp(GameObject propPrefab)
    {
        if (propPrefab == null) return;
        
        // Get tile's mesh bounds (local space) - we need the top of the tile
        float tileTopY = 0f;
        MeshFilter tileMF = GetComponent<MeshFilter>();
        if (tileMF != null && tileMF.sharedMesh != null)
        {
            Bounds tileBounds = tileMF.sharedMesh.bounds;
            // bounds.max.y gives us the top of the mesh in local space
            tileTopY = tileBounds.max.y * transform.localScale.y;
        }
        
        // Get prop's mesh bounds from the PREFAB (before instantiating)
        // We need to know how far the bottom of the prop is from its pivot (center)
        float propBottomOffset = 0f;
        MeshFilter propMF = propPrefab.GetComponent<MeshFilter>();
        if (propMF == null)
        {
            propMF = propPrefab.GetComponentInChildren<MeshFilter>();
        }
        if (propMF != null && propMF.sharedMesh != null)
        {
            Bounds propBounds = propMF.sharedMesh.bounds;
            // bounds.min.y is negative if pivot is at center (bottom is below pivot)
            // We need to offset by the negative of this to bring the bottom up to y=0
            propBottomOffset = -propBounds.min.y * propPrefab.transform.localScale.y;
        }
        
        // Instantiate and position
        GameObject newProp = Instantiate(propPrefab, transform);
        newProp.name = propPrefab.name;
        
        // Final Y = tile top + prop bottom offset (to sit prop's bottom on tile's top)
        float finalY = tileTopY + propBottomOffset;
        
        newProp.transform.localPosition = new Vector3(0, finalY, 0);
        newProp.transform.localRotation = Quaternion.identity;
    }
    
    /// <summary>
    /// Removes all props from this tile.
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

    public Material GetOriginalMaterial()
    {
        if (originalMaterial == null) originalMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        return originalMaterial;
    }

    public Material GetMaterial() => meshRenderer.sharedMaterial;
    public Mesh GetMesh() => meshFilter.sharedMesh;
    public Collider GetCollider() => myCollider;

    private void UpdateCollider(Collider newCollider)
    {
        DestroyImmediate(myCollider);

        if (newCollider is BoxCollider)
        {
            BoxCollider original = newCollider.GetComponent<BoxCollider>();
            BoxCollider myNewCollider = transform.AddComponent<BoxCollider>();

            myNewCollider.center = original.center;
            myNewCollider.size = original.size;
        }

        if (newCollider is MeshCollider)
        {
            MeshCollider original = newCollider.GetComponent<MeshCollider>();
            MeshCollider myNewCollider = transform.AddComponent<MeshCollider>();

            myNewCollider.sharedMesh = original.sharedMesh;
            myNewCollider.convex = original.convex;
        }
    }

    private void UpdateChildren(TileSlot newTile)
    {
        // Clear existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Copy new children
        foreach (Transform child in newTile.transform)
        {
            Instantiate(child.gameObject, transform);
        }
    }
    
    private void UpdateLocalRotation(GameObject referenceObj)
    {
        transform.localRotation = referenceObj.transform.localRotation;
    }

    private void UpdateLocalScale(GameObject referenceObj)
    {
        transform.localScale = referenceObj.transform.localScale;
    }

    public void UpdateLayer(GameObject referenceObj)
    {
        gameObject.layer = referenceObj.layer;
        originalLayerIndex = gameObject.layer;
    }

    public void RotateTile(int dir)
    {
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.y += 90 * dir;
        transform.localEulerAngles = currentRotation;
    }

    public void AdjustY(int verticalDir)
    {
        transform.position += new Vector3(0, 0.1f * verticalDir, 0);
    }
}