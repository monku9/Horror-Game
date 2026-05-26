using UnityEngine;

public class ButtonObject : MonoBehaviour
{
    [Tooltip("Key used to press the button.")]
    public KeyCode buttonKey = KeyCode.E;

    [Tooltip("Player tag used to detect whether the player is in range.")]
    public string playerTag = "Player";

    [Tooltip("Optional manager that requires multiple buttons to be active.")]
    public ButtonRequirement buttonManager;

    [Header("Material")]
    [Tooltip("Change this object's material when the button is pressed.")]
    public bool changeMaterialOnPress = false;

    [Tooltip("Material to apply when the button is pressed.")]
    public Material pressedMaterial;

    public bool IsActive { get; private set; }
    private bool playerInRange;

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(buttonKey))
        {
            PressButton();
        }
    }

    private void PressButton()
    {
        if (IsActive) return;
        IsActive = true;

        if (changeMaterialOnPress && pressedMaterial != null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = pressedMaterial;
            }
        }

        if (buttonManager != null)
        {
            buttonManager.UpdateRequirementState();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
        }
    }
}