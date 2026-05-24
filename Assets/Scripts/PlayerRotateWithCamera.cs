using UnityEngine;

public class PlayerRotateWithCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    void Update()
    {
        if (cameraTransform == null) return;

        Vector3 euler = transform.eulerAngles;
        euler.y = cameraTransform.eulerAngles.y;
        transform.eulerAngles = euler;
    }
}
