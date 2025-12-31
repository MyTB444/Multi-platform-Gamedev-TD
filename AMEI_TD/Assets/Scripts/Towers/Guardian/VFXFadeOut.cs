using UnityEngine;

public class VFXScaleOut : MonoBehaviour
{
    [SerializeField] private float visibleDuration = 1.5f;
    [SerializeField] private float shrinkDuration = 1f;

    private Vector3 originalScale;
    private float timer;

    private void OnEnable()
    {
        originalScale = transform.localScale;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > visibleDuration)
        {
            float shrinkProgress = (timer - visibleDuration) / shrinkDuration;
            float scale = 1f - Mathf.Clamp01(shrinkProgress);
            transform.localScale = originalScale * scale;
        }
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}