using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    public class BillboardUI : MonoBehaviour
    {
        void Start()
        {
            Canvas canvas = GetComponent<Canvas>();

            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main; 
            }
        }
        void LateUpdate()
        {
            Vector3 direction = transform.position - Camera.main.transform.position;
            direction.y = 0f;
            transform.rotation = Quaternion.LookRotation(direction);

        }

    }

}
