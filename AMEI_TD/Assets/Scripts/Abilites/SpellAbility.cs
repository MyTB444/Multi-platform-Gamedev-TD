using System.Collections;
using System.Collections.Generic;
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
    private Vector3 currentMousePosition;
    private Dictionary<Vector3, Vector3> mousePositionDictionary = new();

    public enum SpellType { Physical, Magic, Mechanic, Imaginary }
    private EnemyBase enemyBaseGameObjectRef;
    private Collider[] colliders = new Collider[800];
   
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject flamesPrefab;
    [SerializeField] private GameObject magicAreaPrefab;
    [SerializeField] private GameObject bombPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int flamesPoolSize = 20;
    [SerializeField] private int magicAreaPoolSize = 5;
    [SerializeField] private int bombPoolSize = 3;

   

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

        if (Input.GetKeyDown(KeyCode.E))
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

        if (MagicSpellActivated)
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

        if (ImaginarySpellActivated)
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
                                enemy.isInvisible = false;
                                enemy.UpdateVisuals();
                                Debug.Log($"[Imaginary Spell] Revealed {enemy.name}");
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
                        StartCoroutine(DisableVFX(5f));
                        break;
                    }
                }
            }
        }
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
                        }
                    }

                    if (i >= 2)
                    {
                        if (mousePositionDictionary.Count > i)
                        {
                            Debug.Log("Here");
                            stopMagic = true;
                            CanSelectPaths = false;
                            StartCoroutine(DisableVFX(0f));
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

    private IEnumerator DisableVFX(float WaitTime)
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

    #endregion
}