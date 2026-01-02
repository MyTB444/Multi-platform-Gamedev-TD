using System.Collections.Generic;
using UnityEngine;

public class EnemyVFXPool : MonoBehaviour
{
    private List<GameObject> vfxGameObjects = new();

    public void PoolVFXGameObjects()
    {
        // Collect all child VFX objects
        vfxGameObjects.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            vfxGameObjects.Add(transform.GetChild(i).gameObject);
        }

        // Return them all to pool
        if (vfxGameObjects.Count > 0)
        {
            foreach (GameObject o in vfxGameObjects)
            {
                if (o != null)
                {
                    o.transform.parent = null;
                    ObjectPooling.instance.Return(o);
                }
            }
            vfxGameObjects.Clear();
        }
    }
}