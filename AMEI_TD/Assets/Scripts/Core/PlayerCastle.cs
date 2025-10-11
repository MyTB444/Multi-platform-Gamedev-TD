using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCastle : MonoBehaviour
{
    [SerializeField] private float CastleHealth;
    [SerializeField] private Slider HealthSlider;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != null)
        {
            if (other.gameObject.GetComponent<EnemyBase>() != null)
            {
                if (CastleHealth <= 100f)
                {
                    CastleHealth -= 10f;
                    HealthSlider.value = CastleHealth;
                }
            }
        }
    }

}
