using UnityEngine;

/// <summary>
/// Represents the player's castle/base that enemies are trying to reach.
/// Handles enemy collision detection (damage to player), Guardian tower spawning,
/// and skill loss triggers when enemies breach defenses.
/// </summary>
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

    /// <summary>
    /// Called when an enemy enters the castle's trigger collider.
    /// This means the enemy has reached the player's base and deals damage.
    /// Also triggers skill tree penalties based on the enemy's element type.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // ===== LAYER MASK CHECK =====
        // Bitwise operation to check if the collider's layer is in our enemyLayer mask.
        // 
        // How it works:
        // 1. (1 << other.gameObject.layer) creates a bitmask with only that layer's bit set
        //    Example: layer 8 becomes 0000000100000000 (bit 8 is set)
        // 
        // 2. Bitwise AND (&) with enemyLayer checks if that bit exists in the mask
        //    If result != 0, the layer is included in the mask
        //
        // This is more efficient than string comparison (CompareTag) for physics callbacks
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();

            if (enemy == null) return;

            // Cache enemy data before destroying - we need this for damage/skill calculations
            ElementType enemyElement = enemy.GetElementType();
            int damage = enemy.GetDamage();

            // Remove enemy from game (cleans up references, stops AI, etc.)
            enemy.RemoveEnemy();
            
            // Return to object pool for reuse
            ObjectPooling.instance.Return(other.gameObject);
            
            // Apply damage to player's health
            GameManager.instance.TakeDamageHealth(damage);
            
            // ===== SKILL TREE PENALTY SYSTEM =====
            // When an enemy reaches the castle, the player loses progress on skills
            // of the same element type. This creates meaningful consequences for
            // letting enemies through and encourages diverse tower placement.
            if (SkillTreeManager.instance != null)
            {
                SkillTreeManager.instance.OnEnemyReachedCastle(enemyElement);
            }
        }
    }

    /// <summary>
    /// Spawns the Guardian Tower, which triggers the final Mega Wave when activated.
    /// Called by player action (button/skill) when ready to face the final challenge.
    /// </summary>
    public void SpawnGuardianTower()
    {
        if (guardianTowerPrefab == null)
        {
            Debug.LogWarning("Guardian Tower prefab not assigned!");
            return;
        }

        // Prevent spawning multiple guardians
        if (spawnedGuardian != null)
        {
            Debug.LogWarning("Guardian Tower already spawned!");
            return;
        }

        // Use designated spawn point if available, otherwise spawn above castle
        Vector3 spawnPos = guardianSpawnPoint != null ? guardianSpawnPoint.position : transform.position + Vector3.up * 2f;
        Quaternion spawnRot = guardianSpawnPoint != null ? guardianSpawnPoint.rotation : Quaternion.identity;

        // Instantiate and activate the guardian
        GameObject guardianObj = Instantiate(guardianTowerPrefab, spawnPos, spawnRot);
        spawnedGuardian = guardianObj.GetComponent<TowerGuardian>();

        if (spawnedGuardian != null)
        {
            // ActivateGuardian typically triggers the mega wave via WaveManager
            spawnedGuardian.ActivateGuardian();
        }

        Debug.Log("Guardian Tower Spawned!");
    }

    /// <summary>
    /// Returns whether the Guardian Tower has been spawned.
    /// Used to prevent duplicate spawns and check game progression state.
    /// </summary>
    public bool HasGuardianTower() => spawnedGuardian != null;
    
    /// <summary>
    /// Returns reference to the spawned Guardian Tower (null if not spawned).
    /// </summary>
    public TowerGuardian GetGuardianTower() => spawnedGuardian;
}