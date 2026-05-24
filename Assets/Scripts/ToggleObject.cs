using UnityEngine;

public class ToggleObject : MonoBehaviour
{
    [Tooltip("The object to enable or disable when E is pressed.")]
    public GameObject targetObject;

    [Tooltip("Key used to toggle the object.")]
    public KeyCode toggleKey = KeyCode.E;

    [Tooltip("Player tag used to detect whether the player is in range.")]
    public string playerTag = "Player";

    [Tooltip("Optional manager that requires multiple toggle objects to be active.")]
    public ToggleRequirement toggleManager;

    public bool IsToggled { get; private set; }
    private bool playerInRange;

    private void Start()
    {
        if (targetObject != null)
        {
            IsToggled = targetObject.activeSelf;
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(toggleKey))
        {
            ToggleTarget();
        }
    }

    private void ToggleTarget()
    {
        IsToggled = !IsToggled;

        if (targetObject == null)
        {
            Debug.LogWarning($"ToggleObject: targetObject is not assigned on {gameObject.name}.");
        }
        else
        {
            targetObject.SetActive(IsToggled);
        }

        if (toggleManager != null)
        {
            toggleManager.UpdateRequirementState();
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
