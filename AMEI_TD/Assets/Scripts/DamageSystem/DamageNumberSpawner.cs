using System.Collections;
using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner instance;

    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float randomOffsetRange = 0.3f;
    [SerializeField] private int poolAmount = 50;

    private float accumulatedDamage = 0f;
    private Vector3 lastPosition = Vector3.zero;
    private bool isSuperEffectiveStored = false;
    private bool isNotVeryEffectiveStored = false;
    private bool isImmuneStored = false;
    private Coroutine activeCoroutine = null;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (damageNumberPrefab != null)
        {
            ObjectPooling.instance.Register(damageNumberPrefab, poolAmount);
        }
    }


    public void Spawn(Vector3 position, float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune, bool shouldWaitForDamage = false)
    {
        if (damageNumberPrefab == null) return;

        if (!shouldWaitForDamage)
        {
            // Spawn immediately
            SpawnDamageNumber(position, damage, isSuperEffective, isNotVeryEffective, isImmune);
        }
        else
        {
            // Update latest position
            lastPosition += position;

            // Accumulate damage
            accumulatedDamage += damage;

            // Update effectiveness flags
            if (isSuperEffective) isSuperEffectiveStored = true;
            if (isImmune) isImmuneStored = true;
            if (isNotVeryEffective && !isSuperEffectiveStored) isNotVeryEffectiveStored = true;

            // If no active coroutine, start one
          
                activeCoroutine = StartCoroutine(SpawnDelayed());
            
        }
    }

    private IEnumerator SpawnDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        // Spawn at last recorded position with accumulated damage
        SpawnDamageNumber(lastPosition, accumulatedDamage, isSuperEffectiveStored, isNotVeryEffectiveStored, isImmuneStored);

        // Reset everything
        accumulatedDamage = 0f;
        lastPosition = Vector3.zero;
        isSuperEffectiveStored = false;
        isNotVeryEffectiveStored = false;
        isImmuneStored = false;
        activeCoroutine = null;
    }

    private void SpawnDamageNumber(Vector3 position, float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune)
    {
        // Add random offset so numbers don't stack
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange, randomOffsetRange),
            Random.Range(0f, randomOffsetRange),
            Random.Range(-randomOffsetRange, randomOffsetRange)
        );
        Vector3 spawnPos = position + spawnOffset + randomOffset;

        GameObject numberObj = ObjectPooling.instance.Get(damageNumberPrefab);
        numberObj.transform.position = spawnPos;
        numberObj.transform.rotation = Quaternion.identity;
        numberObj.SetActive(true);

        DamageNumber damageNumber = numberObj.GetComponent<DamageNumber>();
        damageNumber.Setup(damage, isSuperEffective, isNotVeryEffective, isImmune);
    }
}
