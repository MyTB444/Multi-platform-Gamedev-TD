using UnityEngine;

public class PlayerCastle : MonoBehaviour
{
    public static PlayerCastle instance;

    [Header("Guardian Tower")]
    [SerializeField] private GameObject guardianTowerPrefab;
    [SerializeField] private Transform guardianSpawnPoint;

    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;

    private TowerGuardian spawnedGuardian;

    private void Awake()
    {
        instance = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();

            if (enemy == null) return;

            // Get enemy element before removing
            ElementType enemyElement = enemy.GetElementType();
            int damage = enemy.GetDamage();

            enemy.RemoveEnemy();
            ObjectPooling.instance.Return(other.gameObject);
            
            // Deal HP damage
            GameManager.instance.TakeDamageHealth(damage);
            
            // Trigger skill loss based on enemy element
            if (SkillTreeManager.instance != null)
            {
                SkillTreeManager.instance.OnEnemyReachedCastle(enemyElement);
            }
        }
    }

    public void SpawnGuardianTower()
    {
        if (guardianTowerPrefab == null)
        {
            Debug.LogWarning("Guardian Tower prefab not assigned!");
            return;
        }

        if (spawnedGuardian != null)
        {
            Debug.LogWarning("Guardian Tower already spawned!");
            return;
        }

        Vector3 spawnPos = guardianSpawnPoint != null ? guardianSpawnPoint.position : transform.position + Vector3.up * 2f;
        Quaternion spawnRot = guardianSpawnPoint != null ? guardianSpawnPoint.rotation : Quaternion.identity;

        GameObject guardianObj = Instantiate(guardianTowerPrefab, spawnPos, spawnRot);
        spawnedGuardian = guardianObj.GetComponent<TowerGuardian>();

        if (spawnedGuardian != null)
        {
            spawnedGuardian.ActivateGuardian();
        }

        Debug.Log("Guardian Tower Spawned!");
    }

    public bool HasGuardianTower() => spawnedGuardian != null;
    public TowerGuardian GetGuardianTower() => spawnedGuardian;
}