using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCastle : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI pointsUI;
    public static PlayerCastle instance;
    [SerializeField] private int points;


    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {

        points += 200;
    }
    private void Update()
    {
        points = Mathf.Clamp(points, 0, int.MaxValue);
        pointsUI.text = "Points: " + points.ToString();
        if (points <= 0)
        {
            Debug.Log("YouDied");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != null)
        {
            if (other.gameObject.GetComponent<EnemyBase>() != null)
            {

                points -= 30;


            }
        }
    }
    public void AddPoints(int newPoints)
    {
        points += newPoints;
    }
    public void RemovePoints(int newPoints)
    {
        points -= newPoints;
    }

    public int GetPoints() => points;

}
