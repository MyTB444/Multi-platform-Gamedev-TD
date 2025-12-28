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

    private float summonTimer;
    private bool isSummoning = false;
    private List<GameObject> activeMinions = new List<GameObject>();
    private Animator summonerAnimator;

    protected override void Start()
    {
        base.Start();
        summonerAnimator = GetComponent<Animator>();
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

    // Animation event - call when minions should appear
    public void OnSummonAnimationEvent()
    {
        SpawnMinions();
    }

    // Animation event - call at end of summon animation
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
            SpawnMinion(angle);
        }
    }

    private void SpawnMinion(float angle)
    {
        GameObject newMinion = ObjectPooling.instance.GetPoolObject(PoolGameObjectType.EnemyMinion);

        if (newMinion != null)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 desiredPos = transform.position + new Vector3(Mathf.Cos(radians) * spawnRadius, 0, Mathf.Sin(radians) * spawnRadius);
        
            // Find valid NavMesh position
            Vector3 spawnPos = transform.position; // fallback to summoner position
            if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, spawnRadius * 2f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
        
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
}