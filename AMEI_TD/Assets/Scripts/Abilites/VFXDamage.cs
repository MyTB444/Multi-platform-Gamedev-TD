

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

                    StartCoroutine(EnableLiftDamage());
               
                break;  
                    
                case SpellType.Physical:

                   StartCoroutine(EnableFlameDamage(other,2));
                    
                break;
            }
           
        }
    }

    public void SelectedEnemy(EnemyBase enemyBaseRef)
    {

    }

    private Vector3 ReturnRandomPointOnMesh(Bounds bounds)
    {
        return new Vector3(Random.Range(bounds.extents.x / 10f, bounds.extents.x / 7f), Random.Range(bounds.min.y*2.5f, bounds.max.y/3.5f), Random.Range(bounds.extents.z / 10f, bounds.extents.z / 7f));
    }

    private IEnumerator EnableLiftDamage()
    {
        if (enemyBaseGameObjectRef.gameObject != null && enemyBaseGameObjectRef.gameObject.activeInHierarchy)
        {
            enemyBaseGameObjectRef.enemyBaseRef = enemyBaseGameObjectRef;
            enemyBaseGameObjectRef.LiftEffectFunction(true);
            enemies.Add(enemyBaseGameObjectRef);
            enemyBaseGameObjectRef.TakeDamage(1f, true);

            Vector3 startPos = enemyBaseGameObjectRef.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y + 2, startPos.z);
        
            float duration = 1f;
            float elapsed = 0f;
        
      
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
            
          
                enemyBaseGameObjectRef.transform.position = Vector3.Lerp(startPos, targetPos, t);
            
                yield return null; 
            }
        
    
            enemyBaseGameObjectRef.transform.position = targetPos;
        
        
           
        }
    }

    
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
                                            tinyFlames.transform.localPosition = ReturnRandomPointOnMesh(skinnedMeshRenderer[i].localBounds);
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

                    enemyBaseGameObjectRef.TakeDamage(0.0003f, true);

                }

            }

            else if (gameObject.CompareTag("TinyFlames"))
            {
                Debug.Log("Takingdamage");

                enemyBaseGameObjectRef.TakeDamage(0.05f, true);
                
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
                o.LiftEffectFunction(false);
            }
            enemies.Clear();
           
            ObjectPooling.instance.ReturnGameObejctToPool(type, this.gameObject);
        }
    }
}
