using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 30f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
