using UnityEngine;

public class PickupRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private Transform target; // co obracamy (np. sprite)

    private void Awake()
    {
        if (target == null)
            target = transform; // domyślnie obracaj cały obiekt
    }

    private void Update()
    {
        target.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }
}

