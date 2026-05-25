using UnityEngine;
using Mirror;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    private void Start()
    {
        NetworkIdentity identity = GetComponentInParent<NetworkIdentity>();

        // Explicitly disable Camera and AudioListener on non-local players
        Camera cam = GetComponent<Camera>();
        AudioListener listener = GetComponent<AudioListener>();

        if (identity == null || !identity.isLocalPlayer)
        {
            if (cam != null) cam.enabled = false;          // ← Key fix
            if (listener != null) listener.enabled = false; // ← Prevents audio warning too
            enabled = false;
            return;
        }

        // Local player only
        if (cam != null) cam.enabled = true;
        if (listener != null) listener.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}