using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public static Menu instance;
    public GameObject[] levelButtons;
    public Image[] images;
    public Color[] colors;
    public GameObject[] texts;
    void Start()
    {
        instance = this;
    }

    public void EnableLevelButton()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (!levelButtons[i].activeSelf)
                levelButtons[i].SetActive(true);
            else
            {
                levelButtons[i].SetActive(false);
            }
        }
    }
    public void StartLevel1()
    {
        int i = MenuMapRotator.instance.ReturnIndex();
        switch (i)
        {
            case 0:
                SceneManager.LoadScene("Eren");
                break;
            case 1:
                SceneManager.LoadScene("Eren");
                break;
            case 2:
                SceneManager.LoadScene("Eren");
                break;
            case 3:
                SceneManager.LoadScene("Eren");
                break;
                //Add level 1 list 
        }
    }
    public void StartLevel2()
    {
        int i = MenuMapRotator.instance.ReturnIndex();
        switch (i)
        {
            case 0:
                SceneManager.LoadScene("Ilja");
                break;
            case 1:
                SceneManager.LoadScene("Eren");
                break;
            case 2:
                SceneManager.LoadScene("Eren");
                break;
            case 3:
                SceneManager.LoadScene("Eren");
                break;
                //Add level 2 list 
        }
    }
    public void StartLevel3()
    {
        int i = MenuMapRotator.instance.ReturnIndex();
        switch (i)
        {
            case 0:
                SceneManager.LoadScene("Ilja");
                break;
            case 1:
                SceneManager.LoadScene("Ilja");
                break;
            case 2:
                SceneManager.LoadScene("Eren");
                break;
            case 3:
                SceneManager.LoadScene("Ilja");
                break;
                //Add level 3 list 
        }
    }
    public void ChangeButtonColours(int i)
    {
        DisableAllText();
        texts[i].SetActive(true);
        switch (i)
        {
            case 0:
                ColorLoop(colors[0]);
                break;
            case 1:
                ColorLoop(colors[1]);
                break;
            case 2:
                ColorLoop(colors[2]);
                break;
            case 3:
                ColorLoop(colors[3]);
                break;
        }
    }
    private void DisableAllText()
    {
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].SetActive(false);
        }
    }
    public void ColorLoop(Color colour)
    {
        for (int i = 0; i < images.Length; i++)
        {
            images[i].color = colour;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void Quit()
    {
        Application.Quit();
    }
}
