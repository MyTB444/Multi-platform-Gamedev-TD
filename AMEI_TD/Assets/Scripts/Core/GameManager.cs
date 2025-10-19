using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int points;
    public static GameManager instance;
    [SerializeField] private TextMeshProUGUI pointsUI;
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        points += 200;
    }

    // Update is called once per frame
    void Update()
    {
        points = Mathf.Clamp(points, 0, int.MaxValue);
        pointsUI.text = "Points: " + points.ToString();
        if(points > 50)
        {
            pointsUI.color = Color.green;
        }
        if (points <= 50)
        {
            pointsUI.color = Color.red;
            if (points <= 0)
            {
                Debug.Log("YouDied");
            }
        }
        
    }

    public void UpdateSkillPoints(int newPoints)
    {
        points += newPoints;// merged two functions into one //add negative points to remove points and positive to gain points
    }

    public int GetPoints() => points;
}
