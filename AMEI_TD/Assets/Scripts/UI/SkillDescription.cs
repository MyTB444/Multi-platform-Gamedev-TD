using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject descTest;
    public void OnPointerEnter(PointerEventData eventData)
    {
        descTest.SetActive(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        descTest.SetActive(false);
    }
}
