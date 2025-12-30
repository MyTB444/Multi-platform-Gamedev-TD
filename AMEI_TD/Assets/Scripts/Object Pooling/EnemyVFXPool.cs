using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyVFXPool : MonoBehaviour
{

    private List<GameObject> vfxGameObjects = new();
    private ObjectPooling objectPoolingInstance = null;

    private void OnEnable()
    {
        objectPoolingInstance = ObjectPooling.instance;
    }

    public void PoolVFXGameObjects()
    {
        for(int i = 0; i < transform.childCount;i++)
        {
            vfxGameObjects.Add(transform.GetChild(i).gameObject);
        }
        if(vfxGameObjects.Count > 0)
        {
            foreach(GameObject o in vfxGameObjects)
            {
                if (o != null)
                {
                    objectPoolingInstance.ReturnGameObejctToPool(poolType, o);
                    for (int i = 0; i < objectPoolingInstance.listOfPoolInfoRef.Count; i++)
                    {
                        if (poolType == objectPoolingInstance.listOfPoolInfoRef[i].objectType)
                        {
                            o.transform.parent = objectPoolingInstance.listOfPoolInfoRef[i].Container.transform;
                        }
                    }
                }
            }
        }

    }
   
    public PoolGameObjectType poolType { get; set; }
}
