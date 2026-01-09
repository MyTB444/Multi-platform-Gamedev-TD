using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pooling system for reusing GameObjects (enemies, projectiles, VFX) to reduce GC.
/// 
/// Object pooling prevents expensive Instantiate/Destroy calls by recycling objects:
/// 1. Pre-instantiate a pool of inactive objects at startup (prewarm)
/// 2. When needed, Get() an object from the pool instead of Instantiate()
/// 3. When done, Return() the object to the pool instead of Destroy()
/// 
/// Usage:
/// - Call Register() to set up a pool for a prefab (usually in Start)
/// - Call Get() to retrieve an inactive object
/// - Call Return() when done with the object
/// - VFX convenience methods handle positioning and auto-return
/// </summary>
public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling instance;

    /// <summary>
    /// Inspector-editable prewarm configuration.
    /// Allows designers to adjust pool sizes without code changes.
    /// </summary>
    [System.Serializable]
    public class PrewarmItem
    {
        public GameObject prefab;
        public int amount;
    }

    [Header("Prewarm Overrides (edit amounts here)")]
    [SerializeField] private List<PrewarmItem> prewarmList = new List<PrewarmItem>();

    // ===== INTERNAL DATA STRUCTURES =====
    
    // Main pool storage: prefab → queue of available instances
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    
    // Reverse lookup: instance → original prefab (needed for Return())
    private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
    
    // Container transforms to keep hierarchy organized (Pool_EnemyBasic, Pool_Projectile, etc.)
    private Dictionary<GameObject, Transform> containers = new Dictionary<GameObject, Transform>();
    
    // Tracks registered prefabs and their default amounts
    private Dictionary<GameObject, int> registeredPrefabs = new Dictionary<GameObject, int>();
    
    // Ensures prewarm only runs once
    private bool hasPrewarmed = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        DoPrewarm();
    }

    /// <summary>
    /// Registers a prefab with the pool system. Can be called by other scripts
    /// to add prefabs at runtime (e.g., WaveManager registering enemy prefabs).
    /// 
    /// If the prefab is already in the inspector prewarmList, uses that amount.
    /// Otherwise, uses the defaultAmount and adds it to the list for visibility.
    /// </summary>
    /// <param name="prefab">The prefab to pool</param>
    /// <param name="defaultAmount">How many to pre-instantiate</param>
    public void Register(GameObject prefab, int defaultAmount)
    {
        if (prefab == null) return;

        // Check if designer already set a custom amount in inspector
        foreach (var item in prewarmList)
        {
            if (item.prefab == prefab)
            {
                registeredPrefabs[prefab] = item.amount;
                return;
            }
        }

        // Not in inspector list - use code default and add to list
        if (!registeredPrefabs.ContainsKey(prefab))
        {
            registeredPrefabs[prefab] = defaultAmount;
            
            // Add to inspector list so designers can see and adjust it
            prewarmList.Add(new PrewarmItem { prefab = prefab, amount = defaultAmount });
        }
    }

    /// <summary>
    /// Pre-instantiates all registered prefabs at startup.
    /// Spreads the instantiation cost across load time instead of gameplay.
    /// </summary>
    private void DoPrewarm()
    {
        if (hasPrewarmed) return;
        hasPrewarmed = true;

        foreach (var item in prewarmList)
        {
            if (item.prefab == null) continue;

            // Create 'amount' instances and immediately return them to pool
            for (int i = 0; i < item.amount; i++)
            {
                GameObject obj = CreateNewInstance(item.prefab);
                Return(obj);
            }
        }
    }

    /// <summary>
    /// Retrieves an object from the pool. Creates a new instance if pool is empty.
    /// The returned object is INACTIVE - caller must set position/rotation and SetActive(true).
    /// </summary>
    /// <param name="prefab">The prefab type to get</param>
    /// <returns>An inactive GameObject ready for use</returns>
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;

        // Ensure pool exists for this prefab
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        GameObject obj;
        if (pools[prefab].Count > 0)
        {
            // Reuse existing pooled object
            obj = pools[prefab].Dequeue();
        }
        else
        {
            // Pool empty - create new instance (may cause frame spike if frequent)
            obj = CreateNewInstance(prefab);
        }

        return obj;
    }

    /// <summary>
    /// Returns an object to the pool for later reuse.
    /// Automatically deactivates the object.
    /// </summary>
    /// <param name="obj">The object to return (must have been created by this pool)</param>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // Deactivate immediately
        obj.SetActive(false);

        // Look up which prefab this instance came from
        if (instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
            }

            // Prevent duplicate returns (safety check)
            if (!pools[prefab].Contains(obj))
            {
                pools[prefab].Enqueue(obj);
            }
        }
    }

    /// <summary>
    /// Creates a new instance of a prefab for the pool.
    /// Stores the prefab reference for later Return() lookup.
    /// </summary>
    private GameObject CreateNewInstance(GameObject prefab)
    {
        // Get or create container for organization
        Transform container = GetContainer(prefab);
        GameObject obj = Instantiate(prefab, container);

        // Preserve original layer (Unity sometimes changes layer when parenting)
        obj.layer = prefab.layer;

        obj.SetActive(false);
        
        // Store reverse lookup: instance → prefab
        instanceToPrefab[obj] = prefab;
        return obj;
    }

    /// <summary>
    /// Gets or creates an empty container GameObject to hold pooled instances.
    /// Keeps hierarchy organized: ObjectPooling → Pool_EnemyBasic, Pool_Projectile, etc.
    /// </summary>
    private Transform GetContainer(GameObject prefab)
    {
        if (!containers.ContainsKey(prefab))
        {
            GameObject containerObj = new GameObject($"Pool_{prefab.name}");
            containerObj.transform.SetParent(transform);
            containers[prefab] = containerObj.transform;
        }
        return containers[prefab];
    }

    // ==================== VFX CONVENIENCE METHODS ====================
    // These methods handle the common VFX pattern: spawn, position, auto-return after delay

    /// <summary>
    /// Spawns a VFX prefab at a position and automatically returns it to pool after lifetime.
    /// </summary>
    /// <param name="prefab">VFX prefab to spawn</param>
    /// <param name="position">World position</param>
    /// <param name="rotation">Rotation</param>
    /// <param name="lifetime">Seconds before auto-return (0 = manual return required)</param>
    public GameObject GetVFX(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 2f)
    {
        if (prefab == null) return null;

        GameObject vfx = Get(prefab);
        vfx.transform.position = position;
        vfx.transform.rotation = rotation;
        vfx.transform.localScale = prefab.transform.localScale;  // Reset scale
        vfx.SetActive(true);

        if (lifetime > 0)
        {
            StartCoroutine(ReturnAfterDelay(vfx, lifetime));
        }

        return vfx;
    }

    /// <summary>
    /// Spawns a VFX prefab with custom scale.
    /// </summary>
    public GameObject GetVFX(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, float lifetime = 2f)
    {
        if (prefab == null) return null;

        GameObject vfx = Get(prefab);
        vfx.transform.position = position;
        vfx.transform.rotation = rotation;
        vfx.transform.localScale = scale;
        vfx.SetActive(true);

        if (lifetime > 0)
        {
            StartCoroutine(ReturnAfterDelay(vfx, lifetime));
        }

        return vfx;
    }

    /// <summary>
    /// Spawns a VFX prefab as a child of a parent transform.
    /// Useful for effects that should follow an object (e.g., burning enemy).
    /// </summary>
    /// <param name="parent">Transform to parent the VFX to</param>
    /// <param name="lifetime">Seconds before auto-return (-1 = no auto-return)</param>
    public GameObject GetVFXWithParent(GameObject prefab, Transform parent, float lifetime = -1f)
    {
        if (prefab == null) return null;

        GameObject vfx = Get(prefab);
        vfx.transform.SetParent(parent);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;
        vfx.transform.localScale = prefab.transform.localScale;
        vfx.SetActive(true);

        if (lifetime > 0)
        {
            StartCoroutine(ReturnAfterDelay(vfx, lifetime));
        }

        return vfx;
    }

    /// <summary>
    /// Coroutine that returns an object to the pool after a delay.
    /// Used for auto-returning VFX after their animation completes.
    /// </summary>
    private IEnumerator ReturnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Return(obj);
    }
}