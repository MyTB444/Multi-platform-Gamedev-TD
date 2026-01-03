using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SpellFunctionality : MonoBehaviour
{
    [Header("Icons")]
    public Image magicalIcon;
    public Image physicallIcon;
    public Image mechanicalIcon;
    public Image imaginaryIcon;
    [Header("Buttons")]
    public Button physicalButton;
    public Button magicalButton;
    public Button mechanicalButton;
    public Button imaginaryButton;
    public static SpellFunctionality instance;
    void Start()
    {
        instance = this;
    }
    public void EnableButton(SpellEnableType type)
    {
        switch (type)
        {
            case SpellEnableType.Physical:
                physicalButton.interactable = true;
                physicallIcon.color = Color.white;
                break;
            case SpellEnableType.Magical:
                magicalButton.interactable = true;
                magicalIcon.color = Color.white;
                break;
            case SpellEnableType.Mechanical:
                mechanicalButton.interactable = true;
                mechanicalIcon.color = Color.white;
                break;
            case SpellEnableType.Imaginary:
                imaginaryButton.interactable = true;
                imaginaryIcon.color = Color.white;
                break;

        }
    }
}
