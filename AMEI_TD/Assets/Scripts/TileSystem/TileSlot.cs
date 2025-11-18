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
    }

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

    public void UpdateLayer(GameObject referenceObj)
    {
        gameObject.layer = referenceObj.layer;
        originalLayerIndex = gameObject.layer;
    }

    public void RotateTile(int dir)
    {
        transform.Rotate(0, 90 * dir, 0);
    }

    public void AdjustY(int verticalDir)
    {
        transform.position += new Vector3(0, 0.1f * verticalDir, 0);
    }
}