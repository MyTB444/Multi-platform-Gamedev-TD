using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCastle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != null)
        {
            if (other.gameObject.GetComponent<EnemyBase>() != null)
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();

                if (enemy == null) return;

                enemy.RemoveEnemy();

                GameManager.instance.UpdateSkillPoints(-30);
            }
        }
    }
}
