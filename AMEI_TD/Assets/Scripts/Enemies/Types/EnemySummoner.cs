using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySummoner : EnemyBase
{
    [Header("Summoner Settings")]
    [SerializeField] private int maxActiveMinions = 3;
    [SerializeField] private float summonCooldown = 5f;
    [SerializeField] private float spawnRadius = 1.5f;
    [SerializeField] private int minionsPerSummon = 2;
    
    [Header("VFX")]
    [SerializeField] private GameObject magicCirclePrefab;
    [SerializeField] private GameObject smokeSpawnPrefab;
    [SerializeField] private float magicCircleYOffset = 0.1f;
    [SerializeField] private float smokeDelay = 0.3f;
    
    [Header("Minion")]
    [SerializeField] private GameObject minionPrefab;

    private GameObject activeMagicCircle;

    private float summonTimer;
    private bool isSummoning = false;
    private List<GameObject> activeMinions = new List<GameObject>();
    private Animator summonerAnimator;
    private Rigidbody rb;

    protected override void Start()
    {
        base.Start();
        summonerAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        summonTimer = summonCooldown;
    }

    protected override void Update()
    {
        base.Update();
        CleanupDeadMinions();
        HandleSummoning();
    }

    private void HandleSummoning()
    {
        if (isSummoning || isDead) return;

        summonTimer -= Time.deltaTime;

        if (summonTimer <= 0 && activeMinions.Count < maxActiveMinions)
        {
            StartSummoning();
        }
    }

    private void StartSummoning()
    {
        isSummoning = true;
        canMove = false;

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (magicCirclePrefab != null)
        {
            Vector3 circlePos = transform.position + Vector3.up * magicCircleYOffset;
            activeMagicCircle = ObjectPooling.instance.GetVFX(magicCirclePrefab, circlePos, Quaternion.Euler(360, 180, 0), -1f);
            activeMagicCircle.transform.SetParent(transform);
        }

        if (summonerAnimator != null)
        {
            summonerAnimator.SetTrigger("Summon");
        }
        else
        {
            SpawnMinions();
            FinishSummoning();
        }
    }
    
    public void OnSummonAnimationEvent()
    {
        SpawnMinions();
    
        if (activeMagicCircle != null)
        {
            ParticleSystem ps = activeMagicCircle.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
                StartCoroutine(ReturnToPoolAfterDelay(activeMagicCircle, ps.main.startLifetime.constantMax));
            }
            else
            {
                ObjectPooling.instance.Return(activeMagicCircle);
            }
            activeMagicCircle = null;
        }
    }
    
    public void OnSummonAnimationEnd()
    {
        FinishSummoning();
    }

    private void FinishSummoning()
    {
        canMove = true;
        summonTimer = summonCooldown;
        isSummoning = false;
    }

    private void SpawnMinions()
    {
        int minionsToSpawn = Mathf.Min(minionsPerSummon, maxActiveMinions - activeMinions.Count);

        for (int i = 0; i < minionsToSpawn; i++)
        {
            float angle = (360f / minionsToSpawn) * i;
            StartCoroutine(SpawnMinionWithDelay(angle));
        }
    }

    private IEnumerator SpawnMinionWithDelay(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 desiredPos = transform.position + new Vector3(Mathf.Cos(radians) * spawnRadius, 0, Mathf.Sin(radians) * spawnRadius);
        
        Vector3 spawnPos = transform.position;
        if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, spawnRadius * 2f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        
        if (smokeSpawnPrefab != null)
        {
            ObjectPooling.instance.GetVFX(smokeSpawnPrefab, spawnPos, Quaternion.identity, 2f);
        }
        
        yield return new WaitForSeconds(smokeDelay);
        
        GameObject newMinion = ObjectPooling.instance.Get(minionPrefab);

        if (newMinion != null)
        {
            newMinion.SetActive(true);
            newMinion.transform.position = spawnPos;
    
            Vector3[] remainingWaypoints = GetRemainingWaypoints();
            if (remainingWaypoints.Length > 0)
            {
                Vector3 directionToWaypoint = (remainingWaypoints[0] - spawnPos).normalized;
                directionToWaypoint.y = 0;
                if (directionToWaypoint != Vector3.zero)
                {
                    newMinion.transform.rotation = Quaternion.LookRotation(directionToWaypoint);
                }
            }

            EnemyBase minionScript = newMinion.GetComponent<EnemyBase>();
    
            if (minionScript != null)
            {
                minionScript.SetupEnemy(mySpawner, remainingWaypoints);
            }

            mySpawner.GetActiveEnemies().Add(newMinion);
            activeMinions.Add(newMinion);
        }
    }

    private Vector3[] GetRemainingWaypoints()
    {
        int remaining = myWaypoints.Length - currentWaypointIndex;
        
        if (remaining <= 0) return new Vector3[0];

        Vector3[] remainingWaypoints = new Vector3[remaining];
        
        for (int i = 0; i < remaining; i++)
        {
            remainingWaypoints[i] = myWaypoints[currentWaypointIndex + i];
        }

        return remainingWaypoints;
    }

    private void CleanupDeadMinions()
    {
        activeMinions.RemoveAll(minion => minion == null || !minion.activeInHierarchy);
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        // Reset summoning state
        summonTimer = summonCooldown;
        isSummoning = false;

        // Clear active minions list
        activeMinions.Clear();

        // Cleanup any active magic circle VFX
        if (activeMagicCircle != null)
        {
            ObjectPooling.instance.Return(activeMagicCircle);
            activeMagicCircle = null;
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPooling.instance.Return(obj);
    }
}