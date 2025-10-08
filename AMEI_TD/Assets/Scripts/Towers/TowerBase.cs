using UnityEngine;
using Random = UnityEngine.Random;

public class TowerBase : MonoBehaviour
{
    public EnemyBase currentEnemy;

    [SerializeField] private int damage;
    [SerializeField] protected float attackCooldown = 1f;
    protected float lastTimeAttacked;

    [Header("Tower Setup")] 
    [SerializeField] protected Transform towerHead;
    [SerializeField] protected Transform towerBody;
    [SerializeField] protected Transform gunPoint;
    [SerializeField] protected float rotationSpeed = 10f;
    
    [SerializeField] protected float attackRange = 2.5f;
    [SerializeField] protected LayerMask whatIsEnemy;
    [SerializeField] protected LayerMask whatIsTargetable;
    
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float projectileSpeed;
    
    private Collider[] allocatedColliders = new Collider[100];

    protected virtual void Awake()
    {
        
    }
    
    protected virtual void Start()
    {
        
    }
    
    protected virtual void FixedUpdate()
    {
        ClearTargetOutOfRange();
        UpdateTarget();
        HandleRotation();

        if (CanAttack()) AttemptToAttack();
    }

    protected virtual void ClearTargetOutOfRange()
    {
        if (currentEnemy == null) return;
        
        if (Vector3.Distance(currentEnemy.transform.position, transform.position) > attackRange) currentEnemy = null;
    }

    private void UpdateTarget()
    {
        if (currentEnemy == null)
        {
            currentEnemy = FindEnemyWithinRange();
        }
    }
    
    protected virtual EnemyBase FindEnemyWithinRange()
    {
        int enemiesAround =
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, whatIsEnemy);

        if (enemiesAround == 0) return null;
        
        int randomIndex = Random.Range(0, enemiesAround);
        return allocatedColliders[randomIndex].GetComponent<EnemyBase>();
    }

    protected void AttemptToAttack()
    {
        if (!currentEnemy.gameObject.activeSelf)
        {
            currentEnemy = null;
            return;
        }

        Attack();
    }

    protected virtual void Attack()
    {
        lastTimeAttacked = Time.time;
        FireProjectile();
    }
    
    protected virtual void FireProjectile()
    {
        Vector3 directionToEnemy = DirectionToEnemyFrom(gunPoint);

        if (Physics.Raycast(gunPoint.position, directionToEnemy, out RaycastHit hitInfo, Mathf.Infinity,
                whatIsTargetable))
        {
           // Check if hit info has a damageable component.
           // if null then return.
            
            Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.1f;
            GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, gunPoint.rotation);
            newProjectile.GetComponent<TowerProjectileBase>().SetupProjectile(hitInfo.point, damage, projectileSpeed);
        }
    }

    protected virtual bool CanAttack()
    {
        return Time.time > lastTimeAttacked + attackCooldown && currentEnemy != null;
    }

    protected virtual void HandleRotation()
    {
        RotateTowardsEnemy();
        RotateBodyTowardsEnemy();
    }

    protected virtual void RotateTowardsEnemy()
    {
        if (currentEnemy == null || towerHead == null) return;
        
        Vector3 directionToEnemy = DirectionToEnemyFrom(towerHead);

        Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
        
        Vector3 rotation = Quaternion.Lerp(towerHead.rotation, lookRotation, rotationSpeed * Time.deltaTime).eulerAngles;

        towerHead.rotation = Quaternion.Euler(rotation);
    }

    protected void RotateBodyTowardsEnemy()
    {
        if (towerBody == null || currentEnemy == null) return;

        Vector3 directionToEnemy = DirectionToEnemyFrom(towerBody);
        directionToEnemy.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
        towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    protected Vector3 DirectionToEnemyFrom(Transform startPosition)
    {
        return (currentEnemy.transform.position - startPosition.position).normalized;
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
}
