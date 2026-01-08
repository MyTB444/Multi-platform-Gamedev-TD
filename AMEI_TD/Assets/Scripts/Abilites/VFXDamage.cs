using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SpellAbility;
using Random = UnityEngine.Random;

public class VFXDamage : MonoBehaviour
{
    private SpellType spellType;
    private EnemyBase enemyBaseGameObjectRef;

    public bool stopFlames { get; set; } = false;
    private Dictionary<EnemyBase, GameObject> EnemyDictionary = new();

    public bool stopMagic { get; set; } = false;
    private List<SkinnedMeshRenderer> skinnedMeshRenderer = new();
    private ParticleSystem ps;

    private List<EnemyBase> enemiesList = new();


    [Header("VFX Prefabs")]
    [SerializeField] private GameObject tinyFlamesPrefab;

    [Header("Settings")]
    [SerializeField] private float magicDisableTime = 5f;
    [SerializeField] private float tinyFlamesDisableTime = 5f;
    [SerializeField] private float DamageMultiplier = 2f;

    private void Start()
    {
        if (tinyFlamesPrefab != null)
        {
            ObjectPooling.instance.Register(tinyFlamesPrefab, 20);
        }
       
    }
    


    private void OnEnable()
    {

        spellType = instance.currenSpellType;

        enemyBaseGameObjectRef = null;
        EnemyDictionary.Clear();    
        stopFlames = false;
        ps = gameObject.GetComponent<ParticleSystem>();

        if (gameObject.CompareTag("MagicVfx"))
        {
            StartCoroutine(DisableVFXGameObject(magicDisableTime));
        }
    }

    #region FLames


    public IEnumerator EnableFlameDamage(GameObject other, float timeToEnableDamage)
    {
        if (other.gameObject != null && other.gameObject.activeInHierarchy && gameObject != null)
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
                                        tinyFlames = ObjectPooling.instance.Get(tinyFlamesPrefab);
                                        if (tinyFlames != null)
                                        {
                                            tinyFlames.transform.parent = enemyBaseGameObjectRef.vfxContainer.gameObject.transform;
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

                    enemyBaseGameObjectRef.TakeDamage(0f, 0.00003f * DamageMultiplier, true);
                }
            }
        }
    }

    private void DisableTinyFlames(GameObject other)
    {
        if (gameObject != null)
        {
            if (gameObject.CompareTag("TinyFlames"))
            {
                Debug.Log("Takingdamage");
                if (enemyBaseGameObjectRef.isDeadProperty)
                {
                    EnemyDictionary.Remove(enemyBaseGameObjectRef);
                }

                enemyBaseGameObjectRef.TakeDamage(0f, 0.0005f *DamageMultiplier, true);

                if (other.gameObject != null && other.gameObject.activeInHierarchy)
                {
                    StartCoroutine(DisableTinyFlamesAfterDelay(tinyFlamesDisableTime));
                }
            }
        }
    }

    private IEnumerator DisableTinyFlamesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            ObjectPooling.instance.Return(gameObject);
        }
    }




    #endregion

    #region ParticleCollision


    private void OnParticleCollision(GameObject other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            enemyBaseGameObjectRef = other.gameObject.GetComponent<EnemyBase>();
            enemyBaseGameObjectRef.GetRefOfVfxDamageScript(this);

            if (!enemyBaseGameObjectRef.isInvisible)
            {
                switch (spellType)
                {
                    case SpellType.Magic:
                        if (gameObject != null)
                        {
                            StartCoroutine(EnableLiftDamage(enemyBaseGameObjectRef));
                        }
                        break;

                    case SpellType.Physical:
                        if (gameObject != null)
                        {
                            StartCoroutine(EnableFlameDamage(other, 1));
                        }
                        break;
                }
            }
            DisableTinyFlames(other);
        }
    }

    #endregion

    #region RetrunPointOnMesh
    private Vector3 ReturnRandomPointOnMesh(Bounds bounds)
    {
        return new Vector3(bounds.center.x, Random.Range(bounds.extents.y, bounds.extents.y / 3f), Random.Range(-bounds.extents.z / 10f, bounds.extents.z / 8f));
    }
    #endregion

    #region Magic
    public IEnumerator EnableLiftDamage(EnemyBase enemyBase)
    {
        if (enemyBase.gameObject != null && enemyBase.gameObject.activeInHierarchy && enemyBase != null)
        {
           
            enemyBase.LiftEffectFunction(true, false,enemyBase);
            enemiesList.Add(enemyBaseGameObjectRef);

            Vector3 startPos = enemyBase.transform.position;
            Vector3 targetPos = new Vector3(startPos.x, enemyBase.transform.position.y + 2, startPos.z);
        
            float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (enemyBase.transform.position.y <= targetPos.y)
                {
                    enemyBase.transform.position = Vector3.Lerp(startPos, targetPos, t);
                }
                else
                {
                    break;
                }
                    yield return null; 
            }
            enemyBase.transform.position = targetPos;
            duration = 2f;
            elapsed = 0f;
            
            if (enemyBase.transform.position.y >= targetPos.y)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    if (enemyBase != null)
                    {
                        if (!enemyBase.isDeadProperty)
                        {
                            Debug.Log("123");
                            enemyBase.TakeDamage(0f, 0.001f * DamageMultiplier, true);
                        }
                    }
                    if (enemyBase.isDeadProperty)
                    { 
                        enemyBase = null;
                        enemiesList.Remove(enemyBase);
                        break;
                    }
                    yield return null;  
                }
            }
        }
    }
    #endregion

    #region DisableVFX

    private IEnumerator DisableVFXGameObject(float disableTime)
    {
        yield return new WaitForSeconds(disableTime);
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            foreach (EnemyBase o in enemiesList)
            {
                StopCoroutine(EnableLiftDamage(o));
                o.LiftEffectFunction(false, false,o);
            }
            enemiesList.Clear();
           
            ObjectPooling.instance.Return(gameObject);
        }
    }
    #endregion

    #region Getters
    public float GetDamageMultilpier() => DamageMultiplier;
    public List<EnemyBase> GetAffectedEnemyList() => enemiesList;
    #endregion
}