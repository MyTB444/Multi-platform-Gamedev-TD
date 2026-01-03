using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SpellAbility : MonoBehaviour
{
    private SelectedPath selectedPath;
    private List<GameObject> flames = new();
    private GameObject magicAreas;
    public static SpellAbility instance;
    private bool stopFire = false;
    private bool stopMagic = false;
    private bool stopMechanic = false;
    private bool stopImaginary = false;
    private Vector3 currentMousePosition;
    private Dictionary<Vector3, Vector3> mousePositionDictionary = new();

    public enum SpellType { Physical, Magic, Mechanic, Imaginary }
    private EnemyBase enemyBaseGameObjectRef;
    private Collider[] colliders = new Collider[800];
   
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject flamesPrefab;
    [SerializeField] private GameObject magicAreaPrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject imaginaryVFX;

    [Header("Pool Settings")]
    [SerializeField] private int flamesPoolSize = 20;
    [SerializeField] private int magicAreaPoolSize = 5;
    [SerializeField] private int bombPoolSize = 3;
    [SerializeField] private float fireSpellCoolDown;
    [SerializeField] private float magicSpellCoolDown;
    [SerializeField] private float mechanicSpellCoolDown;
    [SerializeField] private float imaginarySpellCoolDown;
    [SerializeField] private float imaginaryVFXCoolDown;
    private List<float> originalColorfloat = new();
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Register prefabs with pool
        if (flamesPrefab != null)
        {
            ObjectPooling.instance.Register(flamesPrefab, flamesPoolSize);
        }
        if (magicAreaPrefab != null)
        {
            ObjectPooling.instance.Register(magicAreaPrefab, magicAreaPoolSize);
        }
        if (bombPrefab != null)
        {
            ObjectPooling.instance.Register(bombPrefab, bombPoolSize);
        }
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
            CanSelectPaths = true;
            currenSpellType = SpellType.Physical;
            stopFire = false;
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            currenSpellType = SpellType.Magic;
            CanSelectPaths = true;
            MagicSpellActivated = true;
            stopMagic = false;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (!MechanicSpellActivated && !stopMechanic)
            {
                currenSpellType = SpellType.Mechanic;
                MechanicSpellActivated = true;
                
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currenSpellType = SpellType.Imaginary;
            ImaginarySpellActivated = true;
        }

        #region FireSpell

        if (FireSpellActivated && currenSpellType == SpellType.Physical)
        {
            
            FireSpell();
        }
        #endregion

        #region MagicSpell

        if (MagicSpellActivated)
        {
            
            MagicSpell();
        }
        #endregion

        #region MechanicSpell

        if (MechanicSpellActivated && !stopMechanic)
        {
            if (Input.GetMouseButton(0))
            {
                EnableMechanicDamage();
            }
        }
        #endregion

        #region ImaginarySpell

        if (ImaginarySpellActivated && !stopImaginary)
        {
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"[Imaginary Spell] Found {enemies.Length} enemies");
    
            if (enemies != null)
            {
                if (enemies.Length > 0)
                {
                    foreach (EnemyBase enemy in enemies)
                    {
                        if (enemy != null)
                        {
                            Debug.Log($"[Imaginary Spell] Enemy: {enemy.name} | isInvisible: {enemy.isInvisible}");
                    
                            if (enemy.isInvisible)
                            {
                                
                                   
                                GameObject vfx = ObjectPooling.instance.Get(imaginaryVFX);
                                if (vfx != null)
                                {
                                    SkinnedMeshRenderer[] skinnedMesh = enemy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                                    if (skinnedMesh != null)
                                    {
                                        foreach (SkinnedMeshRenderer o in skinnedMesh)
                                        {
                                            if (o != null)
                                            {
                                                vfx.transform.parent = enemy.vfxContainer.gameObject.transform;
                                                vfx.transform.localPosition = GiveRandomPointOnEnemyMesh(o.localBounds) - enemy.vfxContainer.gameObject.transform.localPosition;
                                                vfx.transform.rotation = Quaternion.identity;
                                                vfx.SetActive(true);
                                            }
                                        }
                                    }
                                           
                                }        
                              
                                           
                                Debug.Log($"[Imaginary Spell] Revealed {enemy.name}");
                                
                              
                                StartCoroutine(CoolDownSpells(imaginaryVFXCoolDown,enemy,vfx,imaginarySpellCoolDown));//inversed becuase pool particles vfx first then cool down spell 

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
                for (int i = 0; i <= (int)selectedPath.FlameArea.bounds.size.magnitude * 7; i++)
                {
                    GameObject flame = ObjectPooling.instance.Get(flamesPrefab);
                    if (flame != null)
                    {
                        flame.transform.position = GiveRandomPointOnMesh(selectedPath.FlameArea.bounds);
                        flame.transform.rotation = Quaternion.identity;
                        flame.SetActive(true);
                        flames.Add(flame);
                    }

                    if (i >= ((int)selectedPath.FlameArea.bounds.size.magnitude) - 1)
                    {
                        Debug.Log("Here");
                        stopFire = true;
                        CanSelectPaths = false;
                        StartCoroutine(CoolDownSpells(fireSpellCoolDown));
                        break;
                    }
                }
            }
        }
    }

    private Vector3 GiveRandomPointOnEnemyMesh(Bounds bounds)
    {
        return new Vector3(bounds.center.x, Random.Range(bounds.extents.y, bounds.extents.y / 3f), Random.Range(-bounds.extents.z / 10f, bounds.extents.z / 8f));
    }

    private Vector3 GiveRandomPointOnMesh(Bounds bounds)
    {
        if (selectedPath.isOnAxisProperty)
        {
            
            return new Vector3(Random.Range(bounds.min.x + 0.1f, bounds.max.x + 0.1f), bounds.center.y, bounds.center.z);
        }
        return new Vector3(bounds.center.x, bounds.center.y, Random.Range(bounds.min.z + 0.1f, bounds.max.z + 0.1f));
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
                        magicAreas = ObjectPooling.instance.Get(magicAreaPrefab);
                        if (magicAreas != null)
                        {
                            mousePositionDictionary.Add(currentMousePosition, magicAreas.transform.position);
                            magicAreas.transform.localPosition = currentMousePosition;
                            magicAreas.transform.rotation = Quaternion.Euler(-90, 0, 0);
                            magicAreas.SetActive(true);
                            selectedPath = null;
                            CanSelectPaths = true;
                           
                        }
                    }

                    if (i >= 2)
                    {
                        if (mousePositionDictionary.Count > i)
                        {
                            Debug.Log("Here");
                            stopMagic = true;
                            CanSelectPaths = false;
                            StartCoroutine(CoolDownSpells(magicSpellCoolDown));
                           
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
            Vector3 mousepos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousepos);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 400f) & hit.collider != null)
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
                        stopMechanic = true;
                        StartCoroutine(CoolDownSpells(mechanicSpellCoolDown));
                        StartCoroutine(LiftSelectedEnemy(enemyBaseGameObjectRef, false));
                        print($"<color=red> mouseWorldpos </color>" + hit.point);
                        MechanicSpellActivated = false;
                       
                    }
                }
            }
        }
    }

    private IEnumerator LiftSelectedEnemy(EnemyBase enemyBase, bool isExplosiveDamage)
    {
        if (enemyBase.gameObject != null && enemyBase.gameObject.activeInHierarchy && enemyBaseGameObjectRef != null)
        {
            enemyBase.enemyBaseRef = enemyBase;
            enemyBase.LiftEffectFunction(true, true);

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
                elapsed += Time.deltaTime * 2.5f;
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

                    foreach (Color o in enemyBase.originalColors)
                    {
                        enemyBase.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material.color = o;
                    }
                    enemyBase.isInvisible = false;
                    enemyBase.UpdateVisuals();
                   
                    
                    enemyBase.LiftEffectFunction(false, false);
                }
            }
        }
    }

    private void EnableExplosiveDamage(EnemyBase enemyBase)
    {
        if (selectedPath != null)
        {
            StartCoroutine(LiftSelectedEnemy(enemyBase, true));
        }
        else
        {
            GameObject explosionBomb = ObjectPooling.instance.Get(bombPrefab);
            if (explosionBomb != null)
            {
                explosionBomb.transform.position = currentMousePosition;
                explosionBomb.transform.rotation = Quaternion.identity;
                explosionBomb.SetActive(true);

                
                int i = Physics.OverlapSphereNonAlloc(currentMousePosition, 10, colliders);
                
              
                for (int j = 0; j < i; j++)
                {
                    GameObject affectedEnemy = colliders[j].gameObject;
                    if (affectedEnemy != null)
                    {
                        EnemyBase affectedEnemyBaseRef = affectedEnemy.GetComponent<EnemyBase>();
                        if (affectedEnemyBaseRef != null)
                        {
                            enemyBase.Die();
                            
                            StartCoroutine(affectedEnemyBaseRef.ExplodeEnemy(currentMousePosition, affectedEnemyBaseRef));
                            selectedPath = null;
                           
                        }
                    }
                }

                // Return bomb to pool after duration
                StartCoroutine(ReturnToPoolAfterDelay(explosionBomb, 2f));
            }
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
    }

    #endregion

    #region ActivateSpells

    public void ActivateFireSpell()
    {
        FireSpellActivated = true;
        CanSelectPaths = true;
        currenSpellType = SpellType.Physical;
        stopFire = false;
    }

    public void ActivateMagicSpell()
    {
        currenSpellType = SpellType.Magic;
        CanSelectPaths = true;
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
        currenSpellType = SpellType.Imaginary;
        ImaginarySpellActivated = true;
    }

    #endregion

    #region SelectedPath

    public void SelectedPathFromPlayer(SelectedPath path, Vector3 mousePosition)
    {
        selectedPath = path;
        currentMousePosition = mousePosition;
        CanSelectPaths = false;
    }

    #endregion

    #region DisableVFX

    private IEnumerator CoolDownSpells(float WaitTime,EnemyBase enemy = null,GameObject vfx = null,float optionalTime = 0)
    {
        yield return new WaitForSeconds(WaitTime);
        selectedPath = null;
        
        switch (currenSpellType)
        {
            case SpellType.Physical:
                foreach (GameObject o in flames)
                {
                    yield return new WaitForSeconds(0.3f);
                    ObjectPooling.instance.Return(o);
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

            case SpellType.Mechanic:
                CanSelectPaths = false;
                stopMechanic = false;
                MechanicSpellActivated = false;
                break;

            case SpellType.Imaginary:
                enemy.isInvisible = false;
                enemy.UpdateVisuals();

                float elapsed = 0;
                float duration = 3;
                List<ParticleSystem> ps = vfx.GetComponentsInChildren<ParticleSystem>().ToList();
                List<ParticleSystemRenderer> psRenderer = vfx.GetComponentsInChildren<ParticleSystemRenderer>().ToList();
            
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                   
                    for (int i = 0; i < ps.Count; i++) 
                    {
                        var emission = ps[i].emission;
                        emission.enabled = false;
                        originalColorfloat.Add(psRenderer[i].material.GetFloat("_AllColorFactor")); 
                        for (int j = 0; j < originalColorfloat.Count; j++)
                        {
                            originalColorfloat[j] = Mathf.Lerp(1, 0, t);
                            psRenderer[i].material.SetFloat("_AllColorFactor", originalColorfloat[j]);
                        }
                        
                        yield return null;
                    }
                   
                }
                if (elapsed >= duration)
                {
                    originalColorfloat.Clear();
                    ps.Clear();
                    psRenderer.Clear();

                    ObjectPooling.instance.Return(vfx);
                    yield return new WaitForSeconds(optionalTime);
                    CanSelectPaths = false;
                    stopImaginary = false;
                    ImaginarySpellActivated = false;
                   
                }
                break;
        }
    }

    #endregion

    #region Getters

    public bool FireSpellActivated { get; set; }
    public bool MagicSpellActivated { get; set; }
    public bool MechanicSpellActivated { get; set; }
    public bool ImaginarySpellActivated { get; set; }
    public bool CanSelectPaths { get; internal set; }
    public SpellType currenSpellType { get; internal set; }

    public float GetFireSpellCoolDown() => fireSpellCoolDown;

    public float GetMagicSpellCoolDown() => magicSpellCoolDown;

    public float GetMechanicSpellCoolDown() => mechanicSpellCoolDown;

    public float GetImaginarySpellCoolDown() => imaginarySpellCoolDown;

    public float GetImaginaryVFXCoolDown() => imaginaryVFXCoolDown;
    #endregion
}