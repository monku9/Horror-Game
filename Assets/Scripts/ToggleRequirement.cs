using UnityEngine;

public class ToggleRequirement : MonoBehaviour
{
    [Tooltip("Toggle objects that participate in this requirement.")]
    public ToggleObject[] toggleObjects;

    [Tooltip("The target object to enable when the requirement is met.")]
    public GameObject requiredTarget;

    [Tooltip("How many toggle objects must be active to satisfy the requirement.")]
    public int activeCountRequired = 1;

    private void OnValidate()
    {
        if (activeCountRequired < 1)
        {
            activeCountRequired = 1;
        }
    }

    private void Start()
    {
        UpdateRequirementState();
    }

    public void UpdateRequirementState()
    {
        if (requiredTarget == null || toggleObjects == null || toggleObjects.Length == 0)
        {
            return;
        }

        int activeCount = 0;
        foreach (var toggle in toggleObjects)
        {
            if (toggle != null && toggle.IsToggled)
            {
                activeCount++;
            }
        }

        requiredTarget.SetActive(activeCount >= activeCountRequired);
    }
}
