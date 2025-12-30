using System;
using System.Collections.Generic;
using UnityEngine;

public enum PoolGameObjectType
{
    EnemyFast,EnemyBasic,EnemyTank,EnemyInvisible,EnemyReinforced,Flames,MagicArea,TinyFlames,BombPrefab
}
[Serializable]
public class PoolInfo
{
    public PoolGameObjectType objectType;
    public int amount = 0;
    public GameObject gameObjectPrefab;
    public GameObject Container;

    [HideInInspector]
    public List<GameObject> pool = new();

    
    
    

}
public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling instance;
  

    private void Awake()
    {
        instance = this;
        
    }

    [SerializeField] private List<PoolInfo> listOfPool = new();
    public List<PoolInfo> listOfPoolInfoRef => listOfPool;
    private void Start()
    {
        for (int i = 0; i < listOfPool.Count; i++)
        {
            FillPool(listOfPool[i]);
        }
    }

    private void FillPool(PoolInfo info)
    {
        for (int i = 0; i < info.amount; i++)
        {
            GameObject instance = null;
            instance = Instantiate(info.gameObjectPrefab, info.Container.transform);
            instance.SetActive(false);
            info.pool.Add(instance);
        }
    }


    public GameObject GetPoolObject(PoolGameObjectType type)
    {
        PoolInfo selectedPool = GetPoolByType(type);
        List<GameObject> pool = selectedPool.pool;
        GameObject instance = null;

        if (pool.Count > 0)
        {
            instance = pool[pool.Count - 1];
            pool.Remove(instance);
        }
        else
        {
           instance = Instantiate(selectedPool.gameObjectPrefab,selectedPool.Container.transform);
            instance.SetActive(false);
        }
        return instance;
    }

    private PoolInfo GetPoolByType(PoolGameObjectType type)
    {
        for (int i = 0; i < listOfPool.Count; i++)
        {
            if (type == listOfPool[i].objectType)
            {
                return listOfPool[i];
            }
        }
        return null;
    }

    public void ReturnGameObejctToPool(PoolGameObjectType type, GameObject obj)
    {
        PoolInfo selectedPool = GetPoolByType(type);
        List<GameObject> pool = selectedPool.pool;
        if (obj != null)
        {
            obj.SetActive(false);
        }
        if(!pool.Contains(obj))
        {
            pool.Add(obj);
        }

    }


}
