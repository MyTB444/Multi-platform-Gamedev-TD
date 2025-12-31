using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling instance;

    [System.Serializable]
    public class PrewarmItem
    {
        public GameObject prefab;
        public int amount;
    }

    [Header("Prewarm Overrides (edit amounts here)")]
    [SerializeField] private List<PrewarmItem> prewarmList = new List<PrewarmItem>();

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, Transform> containers = new Dictionary<GameObject, Transform>();
    private Dictionary<GameObject, int> registeredPrefabs = new Dictionary<GameObject, int>();
    private bool hasPrewarmed = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        DoPrewarm();
    }

    public void Register(GameObject prefab, int defaultAmount)
    {
        if (prefab == null) return;

        // Check if already in inspector list (user override)
        foreach (var item in prewarmList)
        {
            if (item.prefab == prefab)
            {
                registeredPrefabs[prefab] = item.amount;
                return;
            }
        }

        // Not in list, use script default
        if (!registeredPrefabs.ContainsKey(prefab))
        {
            registeredPrefabs[prefab] = defaultAmount;
            
            // Add to inspector list so user can see/edit it
            prewarmList.Add(new PrewarmItem { prefab = prefab, amount = defaultAmount });
        }
    }

    private void DoPrewarm()
    {
        if (hasPrewarmed) return;
        hasPrewarmed = true;

        foreach (var item in prewarmList)
        {
            if (item.prefab == null) continue;

            for (int i = 0; i < item.amount; i++)
            {
                GameObject obj = CreateNewInstance(item.prefab);
                Return(obj);
            }
        }
    }

    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        GameObject obj;
        if (pools[prefab].Count > 0)
        {
            obj = pools[prefab].Dequeue();
        }
        else
        {
            obj = CreateNewInstance(prefab);
        }

        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);

        if (instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
            }

            if (!pools[prefab].Contains(obj))
            {
                pools[prefab].Enqueue(obj);
            }
        }
    }

    private GameObject CreateNewInstance(GameObject prefab)
    {
        Transform container = GetContainer(prefab);
        GameObject obj = Instantiate(prefab, container);

        // Preserve the prefab's original layer (Unity may change it when parenting)
        obj.layer = prefab.layer;

        obj.SetActive(false);
        instanceToPrefab[obj] = prefab;
        return obj;
    }

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
}