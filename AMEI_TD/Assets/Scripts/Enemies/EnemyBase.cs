using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [SerializeField] float enemyHp;
    [SerializeField] float enemySpeed;
    [SerializeField] Transform centerPoint;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    

    // Get Main Damage
    public virtual void TakeDamage(float damage)
    {
        enemyHp -= damage;
    }

    Vector3 GetCeterPoint()
    {
        return centerPoint.position;
    }
}
