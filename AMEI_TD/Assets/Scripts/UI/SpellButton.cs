using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SpellButton : MonoBehaviour
{
    public int cooldown;
    public  Button button;
    public Image icon;
    public Color activeColor;
    public Color disabledColor;



    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(CooldownStart);
        }

    }
    private void CooldownStart()
    {
        button.interactable = false;
        icon.color = disabledColor;
        StartCoroutine(CooldownDelay());
    }
    private IEnumerator CooldownDelay()
    {
        yield return new WaitForSeconds(cooldown);
        icon.color = activeColor;
        button.interactable = true;
    }
}
