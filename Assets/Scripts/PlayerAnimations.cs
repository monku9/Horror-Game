using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimations : MonoBehaviour
{
    [Tooltip("Animator component that controls the player's animation states.")]
    [SerializeField] private Animator animator;

    [Tooltip("Animator bool parameter used to switch between idle and walk animations.")]
    [SerializeField] private string walkParameter = "isWalking";

    [Tooltip("Use player input as the primary movement check. Falls back to position delta if no input is detected.")]
    [SerializeField] private bool useInputMovement = true;

    [Tooltip("Minimum position delta considered as movement when using transform displacement.")]
    [SerializeField] private float movementThreshold = 0.01f;

    private Vector3 previousPosition;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        previousPosition = transform.position;
    }

    private void Update()
    {
        bool isWalking = IsWalking();
        animator.SetBool(walkParameter, isWalking);
    }

    private bool IsWalking()
    {
        if (useInputMovement)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            if (Mathf.Abs(horizontal) > 0.05f || Mathf.Abs(vertical) > 0.05f)
                return true;
        }

        float distanceMoved = Vector3.Distance(transform.position, previousPosition);
        previousPosition = transform.position;

        return distanceMoved > movementThreshold;
    }
}
