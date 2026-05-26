using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

[Header("Sprint")]
public float sprintMultiplier = 1.5f;
public KeyCode sprintKey = KeyCode.LeftShift;

[Header("Ground Check")]
public float playerHeight;
public LayerMask whatIsGround;
bool grounded;

public Transform orientation;

float horizontalInput;
float verticalInput;
private bool isSprinting;
private float currentMoveSpeed;

Vector3 moveDirection;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb. freezeRotation = true;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();

        // handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }


    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        bool isMoving = horizontalInput != 0 || verticalInput != 0;
        isSprinting = (Input.GetKey(sprintKey) || Input.GetKey(KeyCode.RightShift)) && grounded && isMoving;
        currentMoveSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // limit velocity if needed
        float maxSpeed = currentMoveSpeed;
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }
}
