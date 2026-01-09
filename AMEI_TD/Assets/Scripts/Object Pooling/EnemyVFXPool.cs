using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages VFX cleanup for an enemy. When an enemy dies or is returned to pool,
/// this component ensures all attached VFX (flames, debuff indicators, etc.)
/// are properly unparented and returned to their respective object pools.
/// 
/// Attach to the enemy's VFX container child object (where burn/debuff effects get parented).
/// Call PoolVFXGameObjects() when the enemy is defeated or disabled.
/// </summary>
public class EnemyVFXPool : MonoBehaviour
{
    // Temporary list for collecting child VFX (reused to avoid allocations)
    private List<GameObject> vfxGameObjects = new();

    /// <summary>
    /// Returns all child VFX objects to their respective object pools.
    /// Should be called before the enemy is returned to pool or destroyed.
    /// 
    /// Process:
    /// 1. Collect all current child GameObjects
    /// 2. Unparent each from the enemy (so they don't get disabled with enemy)
    /// 3. Return each to ObjectPooling for reuse
    /// </summary>
    public void PoolVFXGameObjects()
    {
        // Clear list from previous use
        vfxGameObjects.Clear();
        
        // Collect all child VFX objects
        // Note: Iterating forward through children while they exist
        for (int i = 0; i < transform.childCount; i++)
        {
            vfxGameObjects.Add(transform.GetChild(i).gameObject);
        }

        // Return each VFX to its pool
        if (vfxGameObjects.Count > 0)
        {
            foreach (GameObject o in vfxGameObjects)
            {
                if (o != null)
                {
                    // IMPORTANT: Unparent first, then return to pool
                    // If we don't unparent, the VFX would be deactivated
                    // when the enemy is deactivated, and might not reset properly
                    o.transform.parent = null;
                    ObjectPooling.instance.Return(o);
                }
            }
            vfxGameObjects.Clear();
        }
    }
}