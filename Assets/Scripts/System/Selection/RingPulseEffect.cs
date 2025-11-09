using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RingPulseEffect : MonoBehaviour
{
    [Header("Pulsowanie koloru")]
    [Range(0f, 1f)] public float minAlpha = 0.3f;
    [Range(0f, 1f)] public float maxAlpha = 1f;
    public float pulseSpeed = 2f;

    private SpriteRenderer sr;
    private Color baseColor;
    private float time;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    void Update()
    {
        // Oscylacja alphy między min i max
        time += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1f) / 2f);

        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }
}
