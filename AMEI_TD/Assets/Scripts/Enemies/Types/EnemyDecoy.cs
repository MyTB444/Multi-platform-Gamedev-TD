using UnityEngine;
using System.Collections;

public class EnemyDecoy : EnemyBase
{
    [Header("Decoy Settings")]
    [SerializeField] private Material decoyMaterial;
    [SerializeField] private float decoyLifetime = 8f;
    [SerializeField] private float decoySpawnInterval = 5f;
    [SerializeField] private float decoySpawnDelay = 0.5f;
    [SerializeField] private Vector3 decoySpawnOffset1 = new Vector3(0.5f, 0, 0.5f);
    [SerializeField] private Vector3 decoySpawnOffset2 = new Vector3(-0.5f, 0, 0.5f);
    [SerializeField] private GameObject decoySpawnEffectPrefab;

    private bool isDecoy = false;
    private float decoySpawnTime;
    private float lastDecoySpawnTime;

    protected override void Start()
    {
        base.Start();

        if (isDecoy)
        {
            decoySpawnTime = Time.time;
            StopAllCoroutines();
        }
        else
        {
            lastDecoySpawnTime = Time.time;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isDecoy)
        {
            if (Time.time >= decoySpawnTime + decoyLifetime)
            {
                DestroyDecoy();
            }
        }
        else
        {
            if (Time.time >= lastDecoySpawnTime + decoySpawnInterval)
            {
                StartCoroutine(SpawnDecoysWithDelay());
                lastDecoySpawnTime = Time.time;
            }
        }
    }

    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (isDecoy)
        {
            DestroyDecoy();
            return;
        }

        base.TakeDamage(damageInfo);
    }

    public override void TakeDamage(float incomingDamage)
    {
        TakeDamage(new DamageInfo(incomingDamage, ElementType.Physical));
    }

    private IEnumerator SpawnDecoysWithDelay()
    {
        if (decoySpawnEffectPrefab != null)
        {
            Transform spawnPoint = GetBottomPoint() != null ? GetBottomPoint() : transform;
            ObjectPooling.instance.GetVFXWithParent(decoySpawnEffectPrefab, spawnPoint, decoySpawnDelay + 2f);
        }

        yield return new WaitForSeconds(decoySpawnDelay);

        CreateDecoy(decoySpawnOffset1);
        CreateDecoy(decoySpawnOffset2);
    }

    private void CreateDecoy(Vector3 offset)
    {
        GameObject decoyObject = Instantiate(gameObject, transform.position + offset, transform.rotation);

        int decoyLayerIndex = LayerMask.NameToLayer("Decoy");
        if (decoyLayerIndex == -1)
        {
            Debug.LogError("Decoy layer does not exist! Please create it in Project Settings > Tags and Layers");
        }
        SetLayerRecursively(decoyObject, decoyLayerIndex);
        Debug.Log($"Decoy created on layer: {LayerMask.LayerToName(decoyObject.layer)} (index: {decoyObject.layer})");

        decoyObject.SetActive(false);

        EnemyDecoy decoyScript = decoyObject.GetComponent<EnemyDecoy>();
        if (decoyScript != null)
        {
            decoyScript.isDecoy = true;

            RemoveCopiedEffects(decoyObject);

            if (decoyMaterial != null)
            {
                Renderer[] renderers = decoyObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    r.material = decoyMaterial;
                }
            }

            CopyStateToDecoy(decoyScript);

            decoyObject.SetActive(true);
        }
    }

    private void RemoveCopiedEffects(GameObject decoyObject)
    {
        Transform[] allChildren = decoyObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child == decoyObject.transform) continue;

            if (decoySpawnEffectPrefab != null && child.name.Contains(decoySpawnEffectPrefab.name))
            {
                Destroy(child.gameObject);
                continue;
            }

            if (child.name.Contains("(Clone)"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void CopyStateToDecoy(EnemyDecoy decoy)
    {
        var waypointsField = typeof(EnemyBase).GetField("myWaypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (waypointsField != null)
        {
            waypointsField.SetValue(decoy, GetMyWaypoints());
        }

        var waypointIndexField = typeof(EnemyBase).GetField("currentWaypointIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (waypointIndexField != null)
        {
            waypointIndexField.SetValue(decoy, GetCurrentWaypointIndex());
        }

        var spawnerField = typeof(EnemyBase).GetField("mySpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (spawnerField != null)
        {
            spawnerField.SetValue(decoy, GetMySpawner());
        }

        var isDeadField = typeof(EnemyBase).GetField("isDead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isDeadField != null)
        {
            isDeadField.SetValue(decoy, false);
        }

        // Remove shield from decoys - they shouldn't have shields
        var hasShieldField = typeof(EnemyBase).GetField("hasShield", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasShieldField != null)
        {
            hasShieldField.SetValue(decoy, false);
        }

        var shieldHealthField = typeof(EnemyBase).GetField("shieldHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (shieldHealthField != null)
        {
            shieldHealthField.SetValue(decoy, 0f);
        }

        var activeShieldEffectField = typeof(EnemyBase).GetField("activeShieldEffect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (activeShieldEffectField != null)
        {
            activeShieldEffectField.SetValue(decoy, null);
        }
    }

    private void DestroyDecoy()
    {
        RemoveEnemy();

        Destroy(gameObject);
    }

    public new int GetDamage()
    {
        if (isDecoy)
        {
            return 1;
        }
        return base.GetDamage();
    }

    private EnemySpawner GetMySpawner()
    {
        var field = typeof(EnemyBase).GetField("mySpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (EnemySpawner)field.GetValue(this);
        }
        return null;
    }

    private Vector3[] GetMyWaypoints()
    {
        var field = typeof(EnemyBase).GetField("myWaypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (Vector3[])field.GetValue(this);
        }
        return null;
    }

    private int GetCurrentWaypointIndex()
    {
        var field = typeof(EnemyBase).GetField("currentWaypointIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(this);
        }
        return 0;
    }

    protected override void ResetEnemy()
    {
        base.ResetEnemy();

        // Reset decoy state
        isDecoy = false;
        lastDecoySpawnTime = Time.time;

        // Stop any active decoy spawn coroutines
        StopAllCoroutines();
    }
}