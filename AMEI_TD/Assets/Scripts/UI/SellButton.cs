using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SellButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private float shakeSpeed = 5f;        
    public float shakeAmount = 15f;
    private Vector3 originalRotation;

    private TileButton tb;
    private void Awake()
    {
        tb = GetComponentInParent<TileButton>();
        originalRotation = transform.eulerAngles;
        SetDefaultShake();
    }

    void Update()
    {
        float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        float shakeZ = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeAmount;

        transform.eulerAngles = originalRotation + new Vector3(shakeX, 0, shakeZ);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        shakeSpeed = 20.0f;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        SetDefaultShake();
    }
    public void SetDefaultShake()
    {
        shakeSpeed = 5f;

    }
    public void OnPointerDown(PointerEventData eventData)
    {
        tb.DestoryTower();
    }
}
