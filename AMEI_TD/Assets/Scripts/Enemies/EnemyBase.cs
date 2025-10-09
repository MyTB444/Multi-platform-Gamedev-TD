using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [SerializeField] float enemyHp;
    [SerializeField] float enemySpeed;
    [SerializeField] private Transform centerPoint;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (enemyHp <= 0)
        {
            Die();
        }
    }
    

    // Get Main Damage
    public virtual void TakeDamage(float damage)
    {
        enemyHp -= damage;
    }

    public Vector3 GetCeterPoint()
    {
        return centerPoint.position;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
