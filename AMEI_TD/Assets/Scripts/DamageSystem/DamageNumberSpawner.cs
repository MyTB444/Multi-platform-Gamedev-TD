using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner instance;

    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float randomOffsetRange = 0.3f;
    [SerializeField] private int poolAmount = 50;

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
    
    public void Spawn(Vector3 position, float damage, bool isSuperEffective, bool isNotVeryEffective, bool isImmune)
    {
        if (damageNumberPrefab == null) return;

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