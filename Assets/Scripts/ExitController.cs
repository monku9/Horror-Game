using System.Collections;
using UnityEngine;

public class ExitController : MonoBehaviour
{
    [Tooltip("Number of button presses required to disable the exit.")]
    public int requiredPresses = 3;

    [Tooltip("The exit GameObject to enable/disable.")]
    public GameObject exitObject;

    [Tooltip("If true, the exit starts visible. The exit will be disabled when required presses are reached.")]
    public bool exitInitiallyVisible = true;

    int currentPresses = 0;

    void Start()
    {
        if (exitObject != null)
            exitObject.SetActive(exitInitiallyVisible);
    }

    // Called by buttons when pressed
    public void RegisterPress()
    {
        currentPresses++;
        if (currentPresses >= requiredPresses)
        {
            DisableExit();
        }
    }

    void DisableExit()
    {
        if (exitObject != null)
            exitObject.SetActive(false);
    }

    // Optional: call to reset counts (for testing / respawn)
    public void ResetProgress()
    {
        currentPresses = 0;
        if (exitObject != null)
            exitObject.SetActive(exitInitiallyVisible);
    }
}
