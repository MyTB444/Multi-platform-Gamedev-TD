using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellAbility : MonoBehaviour
{
    private SelectedPath selectedPath;
    private List<GameObject> flames = new();
    private GameObject magicAreas;
    public static SpellAbility instance;
    private bool stopFire = false;
    private bool stopMagic = false;
    private Vector3 currentMousePosition;
    private Dictionary<Vector3, Vector3> mousePositionDictionary = new();

    public enum SpellType {Physical,Magic,Mechanic,Imaginary}
    private EnemyBase enemyBaseGameObjectRef;
    private Collider[] colliders = new Collider[20];
    private int MaxResizeArray = 3;
    private int resizeCount;

    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        FireSpellActivated = false;
        MagicSpellActivated = false;

       
    }

    private void Update()
    {
        //for testing only

        //disable this 
        if (Input.GetKeyDown(KeyCode.A))
        {
            FireSpellActivated = false;
            MagicSpellActivated = false;
            MechanicSpellActivated = false;
            CanSelectPaths = false;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {

            FireSpellActivated = true;
            currenSpellType = SpellType.Physical;
            stopFire = false;

        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            currenSpellType = SpellType.Magic;
            MagicSpellActivated = true;
            stopMagic = false;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            currenSpellType = SpellType.Mechanic;
            MechanicSpellActivated = true;

        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            currenSpellType = SpellType.Imaginary;
            ImaginarySpellActivated = true;
        }

        #region FireSpell


        if (FireSpellActivated && currenSpellType == SpellType.Physical)
        {
            CanSelectPaths = true;
            FireSpell();

        }
        #endregion

        #region MagicSpell
        

        if(MagicSpellActivated)
        {
           
            CanSelectPaths = true;
            MagicSpell();
        }
        #endregion

        #region MechanicSpell

        if (MechanicSpellActivated)
        {

            if (Input.GetMouseButton(0))
            {
                EnableMechanicDamage();
            }
            
        }
        #endregion

        #region ImaginarySpell
        
        if(ImaginarySpellActivated)
        {
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            if (enemies != null)
            {
        
                if (enemies.Length > 0)
                {
                    foreach (EnemyBase enemy in enemies)
                    {
                        if (enemy != null)
                        {
                            if (enemy.isInvisible)
                            {
                               
                                enemy.isInvisible = false;
                                enemy.UpdateVisuals();
                                
                            }
                        }
                    }
                }
            }
            ImaginarySpellActivated = false;
        }

        #endregion
    }




    #region FireSpell

    public void FireSpell()
    {

        if (selectedPath != null)
        {
            
            if (!stopFire)
            {
                for (int i = 0; i <= (int)selectedPath.FlameArea.bounds.size.magnitude * 3; i++)
                {


                    flames.Add(ObjectPooling.instance.GetPoolObject(PoolGameObjectType.Flames));                 
                    if (flames.Count > 0 && flames[i] != null)
                    {

                        flames[i].SetActive(true);
                        flames[i].transform.position = GiveRandomPointOnMesh(selectedPath.FlameArea.bounds);
                        flames[i].transform.rotation = Quaternion.identity;



                    }

                    if (i >= ((int)selectedPath.FlameArea.bounds.size.magnitude) - 1)
                    {
                        Debug.Log("Here");
                        stopFire = true;
                        StartCoroutine(DisableVFX(5f));
                        break;
                    }
                }
            }
        }
    }
    private Vector3 GiveRandomPointOnMesh(Bounds bounds)
    {
        return new Vector3(bounds.center.x, bounds.center.y, Random.Range(bounds.min.z, bounds.max.z));
    }

    #endregion

    #region MagicSpell

    public void MagicSpell()
    {
        
        if (selectedPath != null)
        {
            
            if (!stopMagic)
            {
              
                for (int i = 0; i <= 3; i++)
                {
                                         
                    if (!mousePositionDictionary.ContainsKey(currentMousePosition) && currentMousePosition != Vector3.zero)
                    {
                        magicAreas = ObjectPooling.instance.GetPoolObject(PoolGameObjectType.MagicArea);
                        if (magicAreas != null)
                        {
                            
                            mousePositionDictionary.Add(currentMousePosition, magicAreas.transform.position);
                            magicAreas.transform.localPosition = currentMousePosition;
                            magicAreas.transform.rotation = Quaternion.Euler(-90, 0, 0);
                            magicAreas.SetActive(true);
                            selectedPath = null;
                           
                        
                        }


                    }

                    if (i >= 2)
                    {
                        if (mousePositionDictionary.Count > i)
                        {
                            Debug.Log("Here");
                            stopMagic = true;
                            CanSelectPaths = false;
                            StartCoroutine(DisableVFX(5));
                            CanSelectPaths = true;
                            mousePositionDictionary.Clear();
                            break;
                        }
                    }
                   
                }
               
            }

        }
    }

    #endregion

    #region MechanicSpell

    private void EnableMechanicDamage()
    {
        if (MechanicSpellActivated)
        {
            Vector3 mousepos = Input.mousePosition - Camera.main.transform.position;
            Ray ray = Camera.main.ScreenPointToRay(mousepos);

            RaycastHit hit;
           
            if (Physics.Raycast(ray,out hit,400f) & hit.collider != null)
            {

             
                if (hit.collider.gameObject.GetComponent<EnemyBase>() != null)
                {
                    enemyBaseGameObjectRef = hit.collider.gameObject.GetComponent<EnemyBase>();
                    if (!enemyBaseGameObjectRef.isInvisible)
                    {
                        if (enemyBaseGameObjectRef.gameObject.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                        {
                            enemyBaseGameObjectRef.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material.color = new(0, 1, 0, 0.5f);
                        }


                        StartCoroutine(LiftSelectedEnemy(enemyBaseGameObjectRef, false));
                        print($"<color=red> mouseWorldpos </color>" + hit.point);
                        MechanicSpellActivated = false;
                    }

                }
                    
               
            }

        }
    }

    private IEnumerator LiftSelectedEnemy(EnemyBase enemyBase ,bool isExplosiveDamage)
    {
        if (enemyBase.gameObject != null && enemyBase.gameObject.activeInHierarchy && enemyBaseGameObjectRef != null)
        {
            enemyBase.enemyBaseRef = enemyBase;
            enemyBase.LiftEffectFunction(true,true);
           
            //enemyBaseGameObjectRef.TakeDamage(0f, 1f, true);

            Vector3 startPos = enemyBase.transform.position;
            Vector3 targetPos = Vector3.zero;
            if (!isExplosiveDamage)
            {
                targetPos = new Vector3(startPos.x, startPos.y + 4, startPos.z);
            }
            else
            {
               
                
                targetPos = currentMousePosition;
                
            }

            float duration = 1f;
            float elapsed = 0f;


            while (elapsed < duration)
            {
                elapsed += Time.deltaTime*2.5f;
                float t = elapsed / duration;


                enemyBase.transform.position = Vector3.Lerp(startPos, targetPos, t);

                yield return null;
            }
            enemyBase.transform.position = targetPos;

            if (isExplosiveDamage)
            {
               

                CanSelectPaths = false;
                selectedPath = null;
                enemyBase.myBody.isKinematic = true;
                EnableExplosiveDamage(enemyBase);
                yield break;

                
            }
            else
            {
                CanSelectPaths = true;
                elapsed = 0f;
                duration = 5f;
               
                while (elapsed < duration)
                {
                  
                    if (selectedPath != null)
                    {
                       
                        EnableExplosiveDamage(enemyBase);
                        yield break; 
                    }

                    elapsed += Time.deltaTime;
                    yield return null; 
                }

              
                if (selectedPath == null)
                {
                    CanSelectPaths = false;
                    enemyBase.LiftEffectFunction(false, false);
                }
            }

        
        }


    }

    private void EnableExplosiveDamage(EnemyBase enemyBase)
    {
        if(selectedPath != null)
        {
            StartCoroutine(LiftSelectedEnemy(enemyBase, true));
        }
        else
        {
            GameObject ExplosionBomb = ObjectPooling.instance.GetPoolObject(PoolGameObjectType.BombPrefab);
            if (ExplosionBomb != null)
            {
                ExplosionBomb.transform.position = currentMousePosition;
                ExplosionBomb.transform.rotation = Quaternion.identity;
                ExplosionBomb.SetActive(true);

           
                
                int i = Physics.OverlapSphereNonAlloc(currentMousePosition, 5, colliders);
                Debug.Log(i);
                if( i > colliders.Length && resizeCount <= MaxResizeArray)
                {
                    colliders = new Collider[colliders.Length*2];
                    resizeCount++;
                    i = Physics.OverlapSphereNonAlloc(currentMousePosition, 5, colliders);
                }
                for(int j = 0;j< i;j++)
                {
                    GameObject affectedEnemy = colliders[j].gameObject;
                    if (affectedEnemy != null)
                    {
                        EnemyBase affectedEnemyBaseRef  = affectedEnemy.GetComponent<EnemyBase>();
                        if (affectedEnemyBaseRef != null)
                        {
                            
                            enemyBase.Die();
                           
                            StartCoroutine(affectedEnemyBaseRef.ExplodeEnemy(currentMousePosition,affectedEnemyBaseRef));
                            selectedPath = null;
                        }
                        
                    }
                }


            }
        }
        
        
    }



    #endregion


    #region ActivateSpells
    public void ActivateFireSpell()
    {
        FireSpellActivated = true;//Eren has to enable this spell and this variable (bool) in skill tree //for testing only
        currenSpellType = SpellType.Physical;
        stopFire = false;
    }

    public void ActivateMagicSpell()
    {
        currenSpellType = SpellType.Magic;
        MagicSpellActivated = true;
        stopMagic = false;
    }

    public void ActivateMechanicSpell()
    {
        currenSpellType = SpellType.Mechanic;
        MechanicSpellActivated = true;

    }

    public void ActivateImaginarySpell()
    {
        currenSpellType = SpellType.Mechanic;
        ImaginarySpellActivated = true;
    }

    #endregion


    #region SelectedPath
    public void SelectedPathFromPlayer(SelectedPath path,Vector3 mousePosition)
    {
       
        selectedPath = path;       
        currentMousePosition = mousePosition;
        CanSelectPaths = false;
    }
    #endregion


    #region DisableVFX
    private IEnumerator DisableVFX(float WaitTime)
    {
       
        yield return new WaitForSeconds(WaitTime);
        selectedPath = null;
        switch (currenSpellType)
        {
            case SpellType.Physical:
                foreach (GameObject o in flames)
                {
                    yield return new WaitForSeconds(0.3f);//for disbaling fire in a gradual way
                    ObjectPooling.instance.ReturnGameObejctToPool(PoolGameObjectType.Flames, o);
                }
                flames.Clear();
                CanSelectPaths = false;
                FireSpellActivated = false;
               
            break;
            case SpellType.Magic:                
               
                currentMousePosition = Vector3.zero;             
                stopMagic = false;
                CanSelectPaths = false;
                MagicSpellActivated = false;
            break;

        }
       
    }
    #endregion

    #region Getters
    public bool FireSpellActivated {  get; set; }

    public bool MagicSpellActivated { get; set; }

    public bool MechanicSpellActivated { get; set; }

    public bool ImaginarySpellActivated { get; set; }

    public bool CanSelectPaths { get; internal set; }

    public SpellType currenSpellType { get; internal set; }

    #endregion
}
