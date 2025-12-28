
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum EnemyType
{
    Basic,
    Fast,
    Tank,
    Invisible,
    Reinforced,
}


public class EnemyBase : MonoBehaviour, IDamageable
{
    protected EnemySpawner mySpawner;

    [Header("Enemy Stats")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float enemySpeed;
    [SerializeField] private int damage;
    [SerializeField] private int reward;
    public float enemyMaxHp = 100;

    [Header("Ability")]
    [SerializeField] private bool isInvisible;
    [SerializeField] private bool isReinforced;

    [Header("Visuals")]
    [SerializeField] private Color enemyColor = Color.white; 

    [Header("Path")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Transform bottomPoint;

    private NavMeshAgent NavAgent;
    private Animator EnemyAnimator;


    private float enemyCurrentHp;
    protected bool isDead;

    protected Vector3[] myWaypoints;
    protected int currentWaypointIndex = 0;
    private float waypointReachDistance = 0.3f;

    private int originalLayerIndex;
    private float totalDistance;

    private Vector3 Destination;
    [HideInInspector]public Rigidbody myBody;
    private bool spellsActivated;


   

    private VFXDamage vfxDamageScriptRef;

    [SerializeField] private Canvas enemyHealthDisplayCanvas;
    [SerializeField] private Slider healthBar;

    [SerializeField] private EnemyVFXPool enemyVFXPoolScriptRef;

    
    private void OnEnable()
    {
        enemyHealthDisplayCanvas.enabled  = false;
        if (enemyBaseRef == null)
        {
            enemyBaseRef = this;
        }
        vfxContainer = enemyVFXPoolScriptRef;
        vfxContainer.PoolVFXGameObjects();

        enemyHealthDisplayCanvas.worldCamera = Camera.main; 
        healthBar.value = 1;
       
        UpdateVisuals();
        //Renderer renderer = GetComponent<Renderer>();
        NavAgent = GetComponent<NavMeshAgent>();
        EnemyAnimator = GetComponent<Animator>();
        myBody = GetComponent<Rigidbody>();
        NavAgent.enabled = false;
    }

    private void Awake()
    {
        originalLayerIndex = gameObject.layer;
     
    }

    public void GetRefOfVfxDamageScript(VFXDamage vfxDamageScriptRef)
    {
        this.vfxDamageScriptRef = vfxDamageScriptRef;
        if (this.vfxDamageScriptRef != null)
        {
            this.vfxDamageScriptRef.stopFlames = false;
        }
    }

   

    protected virtual void Update()
    {
        if (!enemyBaseRef.spellsActivated)
        {
            FollowPath();
          
        }
        PlayAnimations();
    }
    
    public void SetupEnemy(EnemySpawner myNewSpawner)
    {
        mySpawner = myNewSpawner;
        UpdateWaypoints(myNewSpawner.currentWaypoints);
        CollectTotalDistance();
        ResetEnemy();
        BeginMovement();
    }

    private void UpdateWaypoints(Vector3[] newWaypoints)
    {
        myWaypoints = new Vector3[newWaypoints.Length];
        for (int i = 0; i < myWaypoints.Length; i++)
        {
            myWaypoints[i] = newWaypoints[i];
        }
    }

    private void BeginMovement()
    {
        currentWaypointIndex = 0;
    }

    private void CollectTotalDistance()
    {
        totalDistance = 0;
        for (int i = 0; i < myWaypoints.Length; i++)
        {
            if (i == myWaypoints.Length - 1) break;
            float distance = Vector3.Distance(myWaypoints[i], myWaypoints[i + 1]);
            totalDistance += distance;
        }
    }

    private void FollowPath()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length)
        {
            ReachedEnd();
            return;
        }

        Vector3 targetWaypoint = myWaypoints[currentWaypointIndex];
        if (!bottomPoint) return;

        Vector3 direction = (targetWaypoint - bottomPoint.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        Destination = direction * (enemySpeed * Time.deltaTime);
        transform.position += Destination;
       
        float distanceToWaypoint = Vector3.Distance(bottomPoint.position, targetWaypoint);
        if (distanceToWaypoint <= waypointReachDistance)
        {
          
            currentWaypointIndex++;
        }
        //print($"<color=red>{currentWaypointIndex}</color>");
        NavAgent.enabled = true;
        if (NavAgent.isActiveAndEnabled && NavAgent.isOnNavMesh)
        {
            NavAgent.SetDestination(Destination);

        }
    }
    private void PlayAnimations()
    {
        if (!spellsActivated)
        {
            EnemyAnimator.SetBool("Walk", true);
            myBody.constraints = RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            
            EnemyAnimator.SetBool("Walk", false);
            EnemyAnimator.SetTrigger("FloatMidAir");
           
        }
    }
    


    public float GetRemainingDistance()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length) return 0;

        float remainingDistance = 0;
        remainingDistance += Vector3.Distance(transform.position, myWaypoints[currentWaypointIndex]);

        for (int i = currentWaypointIndex; i < myWaypoints.Length - 1; i++)
        {
            remainingDistance += Vector3.Distance(myWaypoints[i], myWaypoints[i + 1]);
        }
        return remainingDistance;
    }

    

   
    private void ReachedEnd()
    {
        Destroy(gameObject);
    }

    public virtual void TakeDamage(float damage,bool spellDamageEnabled = false, bool isAntiInvisible = false, bool isAntiReinforced = false)
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            
            enemyHealthDisplayCanvas.enabled = true;
            if (isInvisible && !isAntiInvisible)
            {
                return;
            }

            if (isReinforced && !isAntiReinforced)
            {
                return;
            }

            enemyCurrentHp -= damage;
            if (spellDamageEnabled)
            {
                
                healthBar.value -= damage * Time.fixedDeltaTime * 10;
                StartCoroutine(DisableHealthBar(false));
            }
            else
            {
                
                healthBar.value -= damage;
                StartCoroutine(DisableHealthBar(false));
            }



            if ((enemyCurrentHp <= 0 || healthBar.value <= 0) && !isDead)
            {
                isDead = true;

                Die();
            }
            if (gameObject.activeInHierarchy && gameObject != null)
            {
               
                    StartCoroutine(DisableHealthBar(true));
                
            }
        }
    }

    private IEnumerator DisableHealthBar(bool status)
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
           
         
            if (status)
            {
                yield return new WaitForSeconds(8f);
               
                
            }
            enemyHealthDisplayCanvas.enabled = !status;



        }
    }








    private void Die()
    {
       
        //Destroy(gameObject);
       
        if (GameManager.instance != null) 
        {
            GameManager.instance.UpdateSkillPoints(reward);
        }
        RemoveEnemy();
        ObjectPooling.instance.ReturnGameObejctToPool(GetEnemyTypeForPooling(), gameObject);
    }
 
    
    private PoolGameObjectType GetEnemyTypeForPooling()
    {
        switch (enemyType)
        {
            case EnemyType.Basic:
                return PoolGameObjectType.EnemyBasic;

            case EnemyType.Fast:
                return PoolGameObjectType.EnemyFast;

            case EnemyType.Tank:
                return PoolGameObjectType.EnemyTank;

            case EnemyType.Invisible:
                return PoolGameObjectType.EnemyInvisible;

            case EnemyType.Reinforced:
                return PoolGameObjectType.EnemyReinforced;
        }
        return 0;
    }
    public void RemoveEnemy()
    {
        if (mySpawner != null) mySpawner.RemoveActiveEnemy(gameObject);
    }

    private void ResetEnemy()
    {
        gameObject.layer = originalLayerIndex;
        enemyCurrentHp = enemyMaxHp;
        isDead = false;
        UpdateVisuals();
    }

    

  

    public void LiftEffectFunction(bool status)
    {

        if (gameObject.activeInHierarchy && gameObject != null)
        {
            enemyBaseRef.myBody.useGravity = !status;
            if (!status)
            {
                myBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                StartCoroutine(EnableNavMesh());

            }
            else
            {
                enemyBaseRef.spellsActivated = status;
                enemyBaseRef.NavAgent.enabled = !status;
                StartCoroutine(DisableYPos());


            }
        }
           
    }

    private void OnMouseDown()
    {
        if (SpellAbility.instance.MechanicSpellActivated)
        {
            vfxDamageScriptRef.SelectedEnemy(this);
        }
    }

    private IEnumerator DisableYPos()
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            yield return new WaitForSeconds(0.5f);
            myBody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
    }
    


    private IEnumerator EnableNavMesh()
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            spellsActivated = false;
            enemyBaseRef.NavAgent.enabled = true;
        }
    }
  
    private void UpdateVisuals()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) return;
        
        Color finalColor = enemyColor;
        
        if (isInvisible)
        {
            finalColor.a = 0.3f;
        }
        else
        {
            finalColor.a = 1f;
        }
        r.material.color = finalColor;
    }

    //Getters
    public Vector3 GetCenterPoint() => centerPoint.position;
    public EnemyType GetEnemyType() => enemyType;
    public float GetEnemyHp() => enemyCurrentHp;
    public Transform GetBottomPoint() => bottomPoint;
    public bool IsInvisible() => isInvisible;
    public bool IsReinforced() => isReinforced;
    public int GetDamage() => damage;

    public EnemyVFXPool vfxContainer { get; set; }
    public EnemyBase enemyBaseRef { get; set; }
}