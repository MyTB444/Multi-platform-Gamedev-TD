using System.Collections;
using UnityEngine;

public class EnemySplitter : EnemyBase
{
    [Header("Split Settings")]
    [SerializeField] private GameObject splitVFXPrefab;
    [SerializeField] private int maxSplitLevel = 2;
    [SerializeField] private int currentSplitLevel = 0;
    [SerializeField] private float splitDelayAfterDeath = 0.2f;

    [Header("Split Stats Multipliers")]
    [SerializeField] private float healthMultiplierPerSplit = 0.5f;
    [SerializeField] private float scaleMultiplierPerSplit = 0.7f;
    [SerializeField] private float speedMultiplierPerSplit = 1.2f;
    [SerializeField] private int splitCount = 2;

    public override void TakeDamage(DamageInfo damageInfo)
    {
        float hpBeforeDamage = enemyCurrentHp;
        bool wasAlive = !isDead;
        bool canSplit = currentSplitLevel < maxSplitLevel;

        base.TakeDamage(damageInfo);

        bool justDied = wasAlive && isDead && hpBeforeDamage > 0;

        if (justDied && canSplit)
        {
            SpawnSplits();
        }
    }

    private void SpawnSplits()
    {
        if (splitVFXPrefab != null)
        {
            Instantiate(splitVFXPrefab, transform.position, Quaternion.identity);
        }

        for (int i = 0; i < splitCount; i++)
        {
            SpawnSplitEnemy(i);
        }

        Debug.Log($"[EnemySplitter] Spawned {splitCount} splits at level {currentSplitLevel + 1}");
    }

    private void SpawnSplitEnemy(int index)
    {
        Vector3 spawnOffset = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0,
            Random.Range(-0.5f, 0.5f)
        );
        Vector3 spawnPosition = transform.position + spawnOffset;

        GameObject newEnemy = Instantiate(gameObject, spawnPosition, transform.rotation);

        newEnemy.SetActive(true);

        float newMaxHp = enemyMaxHp * healthMultiplierPerSplit;
        float newScale = transform.localScale.x * scaleMultiplierPerSplit;

        newEnemy.transform.localScale = Vector3.one * newScale;

        EnemySplitter splitterScript = newEnemy.GetComponent<EnemySplitter>();
        if (splitterScript != null)
        {
            splitterScript.currentSplitLevel = currentSplitLevel + 1;
        }

        EnemyBase enemyScript = newEnemy.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
            enemyScript.enemyMaxHp = newMaxHp;

            var baseType = typeof(EnemyBase);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            var hpField = baseType.GetField("enemyCurrentHp", bindingFlags);
            if (hpField != null)
            {
                hpField.SetValue(enemyScript, newMaxHp);
            }

            var isDeadField = baseType.GetField("isDead", bindingFlags);
            if (isDeadField != null)
            {
                isDeadField.SetValue(enemyScript, false);
            }

            var spawnerField = baseType.GetField("mySpawner", bindingFlags);
            if (spawnerField != null && mySpawner != null)
            {
                spawnerField.SetValue(enemyScript, mySpawner);
            }

            var waypointsField = baseType.GetField("myWaypoints", bindingFlags);
            if (waypointsField != null && myWaypoints != null)
            {
                waypointsField.SetValue(enemyScript, myWaypoints);
            }

            var waypointIndexField = baseType.GetField("currentWaypointIndex", bindingFlags);
            if (waypointIndexField != null)
            {
                waypointIndexField.SetValue(enemyScript, currentWaypointIndex);
            }
        }
    }
}
