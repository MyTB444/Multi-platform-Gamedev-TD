using UnityEngine;

public class MenuMapShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float amplitude = 0.1f;    // Maximum offset from the original position
    public float frequency = 0.5f;    // Speed of the shake

    private Vector3 initialPosition;
    private float timeOffset;

    void Start()
    {
        // Save the starting position
        initialPosition = transform.localPosition;

        // Add random phase offset so multiple objects don't shake in sync
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        float time = Time.time * frequency + timeOffset;

        // Smooth shaking using sin and cos for 2D shake
        float offsetX = Mathf.Sin(time) * amplitude;
        float offsetY = Mathf.Cos(time) * amplitude;

        transform.localPosition = initialPosition + new Vector3(offsetX, offsetY, 0f);
    }
}
