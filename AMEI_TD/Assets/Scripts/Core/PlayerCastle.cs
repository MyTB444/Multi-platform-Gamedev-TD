using UnityEngine;

public class PlayerCastle : MonoBehaviour
{
    // When an enemy reaches the castle, remove it and deduct skill points
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != null)
        {
            if (other.gameObject.GetComponent<EnemyBase>() != null)
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();

                if (enemy == null) return;

                enemy.RemoveEnemy();

                // Penalty for enemy reaching the castle
                GameManager.instance.UpdateSkillPoints(-enemy.GetDamage());
            }
        }
    }
}