

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using static SpellAbility;
using Random = UnityEngine.Random;

public class VFXDamage : MonoBehaviour
{
    private SpellType spellType;
    private EnemyBase enemyBaseGameObjectRef;

    public bool stopFlames = false;
    private Dictionary<EnemyBase, GameObject> EnemyDictionary = new();
   
   
    private List<SkinnedMeshRenderer> skinnedMeshRenderer = new();
    private ParticleSystem ps;

    private List<EnemyBase> enemies = new();

 

 
    private void OnEnable()
    {
        spellType =   instance.currenSpellType;
        enemyBaseGameObjectRef = null;
        EnemyDictionary.Clear();    
        stopFlames = false;
        ps = gameObject.GetComponent<ParticleSystem>();

        if (spellType == SpellType.Magic)
        {
            StartCoroutine(DisableVFXGameObject(5f,PoolGameObjectType.MagicArea));

        }
    }

    
   

    private void OnParticleCollision(GameObject other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
           
            enemyBaseGameObjectRef = other.gameObject.GetComponent<EnemyBase>();
            enemyBaseGameObjectRef.GetRefOfVfxDamageScript(this);
            switch (spellType)
            {
                case SpellType.Magic:

                    StartCoroutine(EnableLiftDamage(enemyBaseGameObjectRef));
               
                break;  
                    
                case SpellType.Physical:

                   StartCoroutine(EnableFlameDamage(other,1));
                    
                break;

               

            }
           
        }
    }

    

    private Vector3 ReturnRandomPointOnMesh(Bounds bounds)
    {
        return new Vector3(bounds.center.x, Random.Range(bounds.extents.y, bounds.extents.y / 3f), Random.Range(-bounds.extents.z / 10f, bounds.extents.z / 8f));
    }

    private IEnumerator EnableLiftDamage(EnemyBase enemyBase)
    {
        if (enemyBase.gameObject != null && enemyBase.gameObject.activeInHierarchy)
        {
            enemyBase.enemyBaseRef = enemyBase;
            enemyBase.LiftEffectFunction(true,false);
            enemies.Add(enemyBaseGameObjectRef);
            //enemyBaseGameObjectRef.TakeDamage(0f, 1f, true);

            Vector3 startPos = enemyBase.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y + 2, startPos.z);
        
            float duration = 1f;
            float elapsed = 0f;
        
      
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
            
          
                enemyBase.transform.position = Vector3.Lerp(startPos, targetPos, t);
            
                yield return null; 
            }
            enemyBase.transform.position = targetPos;
            duration = 2f;
            elapsed = 0f;
            
            if (enemyBase.transform.position != startPos)
            {
               
                while (elapsed < duration)
                {
                 

                    elapsed += Time.deltaTime;


                    enemyBase.TakeDamage(0f, 0.0000004f, true);
                    
                    yield return null;  
                 
                }
            }
        
    
            
        
        
           
        }
    }

    //fix this //add enemybaseref as parameter in this fucntion
    private IEnumerator EnableFlameDamage(GameObject other,float timeToEnableDamage)
    {
        if (other.gameObject != null && other.gameObject.activeInHierarchy)
        {
            if (!gameObject.CompareTag("TinyFlames"))
            {
                yield return new WaitForSeconds(timeToEnableDamage);

                if (other.gameObject != null)
                {
                    skinnedMeshRenderer = other.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();

                    GameObject tinyFlames;

                   

                    for (int i = 0; i < skinnedMeshRenderer.Count; i++)
                    {

                        if (!EnemyDictionary.ContainsKey(enemyBaseGameObjectRef))
                        {

                            if (!stopFlames)
                            {
                               
                               
                                if (skinnedMeshRenderer[i] != null && enemyBaseGameObjectRef.vfxContainer.gameObject != null)
                                {

                                    Debug.Log("found");
                                    for (int j = 0; j <= 4; j++)
                                    {
                                        tinyFlames = ObjectPooling.instance.GetPoolObject(PoolGameObjectType.TinyFlames);
                                        if (tinyFlames != null)
                                        {                                         
                                            tinyFlames.transform.parent = enemyBaseGameObjectRef.vfxContainer.gameObject.transform;
                                            enemyBaseGameObjectRef.vfxContainer.poolType = PoolGameObjectType.TinyFlames;
                                            tinyFlames.transform.localPosition = ReturnRandomPointOnMesh(skinnedMeshRenderer[i].localBounds) - enemyBaseGameObjectRef.vfxContainer.gameObject.transform.localPosition;
                                            tinyFlames.transform.rotation = Quaternion.Euler(-90, 0, 0);
                                            tinyFlames.SetActive(true);
                                        }
                                    }
                                }

                                

                            }

                        }
                        if (!EnemyDictionary.ContainsKey(enemyBaseGameObjectRef))
                        {
                            EnemyDictionary.Add(enemyBaseGameObjectRef, other);
                        }
                    }
                }
                if (other.gameObject != null && other.gameObject.activeInHierarchy)
                {
                    if (enemyBaseGameObjectRef.isDeadProperty)
                    {
                        EnemyDictionary.Remove(enemyBaseGameObjectRef);
                    }

                    enemyBaseGameObjectRef.TakeDamage(0f,0.0000003f, true);
                   

                }

            }

            else if (gameObject.CompareTag("TinyFlames"))
            {
                Debug.Log("Takingdamage");
                if (enemyBaseGameObjectRef.isDeadProperty)
                {
                    EnemyDictionary.Remove(enemyBaseGameObjectRef);
                }

                enemyBaseGameObjectRef.TakeDamage(0f,0.00005f, true);
               
                
                    if (other.gameObject != null && other.gameObject.activeInHierarchy)
                    {
                        StartCoroutine(DisableVFXGameObject(5f, PoolGameObjectType.TinyFlames));
                    }
                
            }
        }
    }

    


    private IEnumerator DisableVFXGameObject(float disableTime,PoolGameObjectType type)
    {
        yield return new WaitForSeconds(disableTime);
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            foreach(EnemyBase o in enemies)
            {
                o.LiftEffectFunction(false,false);
            }
            enemies.Clear();
           
            ObjectPooling.instance.ReturnGameObejctToPool(type, this.gameObject);
        }
    }
}
