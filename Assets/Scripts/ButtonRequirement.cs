using UnityEngine;

public class ButtonRequirement : MonoBehaviour
{
    [Tooltip("Buttons that participate in this requirement.")]
    public ButtonObject[] buttonObjects;

    [Tooltip("The target object to enable when the requirement is met.")]
    public GameObject requiredTarget;

    [Tooltip("How many buttons must be active to satisfy the requirement.")]
    public int activeCountRequired = 1;

    private void OnValidate()
    {
        if (activeCountRequired < 1)
        {
            activeCountRequired = 1;
        }
    }

    public void UpdateRequirementState()
    {
        if (requiredTarget == null || buttonObjects == null || buttonObjects.Length == 0)
        {
            return;
        }

        int activeCount = 0;
        foreach (var button in buttonObjects)
        {
            if (button != null && button.IsActive)
            {
                activeCount++;
            }
        }

        if (activeCount >= activeCountRequired)
        {
            requiredTarget.SetActive(false);
        }
    }
}