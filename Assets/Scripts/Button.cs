using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Button : MonoBehaviour
{
    [Tooltip("If true, the button only registers once and then becomes inactive.")]
    public bool oneTime = true;

    [Tooltip("Optional visual/locked GameObject to toggle when pressed.")]
    public GameObject pressedVisual;

    [Tooltip("Optional ExitController to notify. If null, set via Inspector or other code.")]
    public ExitController exitController;

    [Tooltip("Event invoked when this button is pressed.")]
    public UnityEvent onPressed;

    bool pressed = false;

    void Reset()
    {
        // Ensure collider is a trigger so player can press by walking into it
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Simple example: press when player (tagged "Player") touches the button
        if (!pressed && other.CompareTag("Player"))
        {
            Press();
        }
    }

    public void Press()
    {
        if (pressed && oneTime) return;
        pressed = true;

        if (pressedVisual != null)
            pressedVisual.SetActive(true);

        onPressed?.Invoke();

        if (exitController != null)
            exitController.RegisterPress();

        if (oneTime)
        {
            // disable collider so it cannot be pressed again
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}

