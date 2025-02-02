// Adeer Khan 20244046
// Followed Tutorial: FIRST PERSON MOVEMENT in 10 MINUTES - Unity Tutorial (https://www.youtube.com/watch?v=f473C43s8nE)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f; // Adjust as needed
    public float groundDrag = 6f; // Lower drag for slipperier movement
    public float jumpForce = 5f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.4f;
    private bool readyToJump = true;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    private bool grounded;

    public Transform groundCheck; // Ground check position
    public float groundDistance = 0.3f; // Radius for ground detection sphere

    [Header("Orientation")]
    public Transform orientation;

    [Header("Animations")]
    [SerializeField] private Animator _animator;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.drag = groundDrag; // Set initial drag

        // Snap player to ground if detected during initialization
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, whatIsGround))
        {
            transform.position = hit.point + Vector3.up * playerHeight / 2; // Adjust height as needed
        }
        else
        {
            Debug.LogWarning("No ground detected during Start. Check collider and layer setup.");
        }
    }

    void Update()
    {
        // Ground check using Physics.Raycast
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + groundDistance, whatIsGround);

        // Update Animator ground status
        if (_animator != null)
        {
            _animator.SetBool("IsGrounded", grounded);
        }

        if (!grounded)
        {
            Debug.LogWarning("No ground detected. Ensure the terrain is assigned to the correct layer.");
        }

        MyInput();
        SpeedControl();

        // Update drag
        rb.drag = grounded ? groundDrag : 0;

        // Update Animator speed
        if (_animator != null)
        {
            float currentSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
            _animator.SetFloat("Speed", currentSpeed);
        }

        // Debug Animator speed
        // Debug.Log("Animator Speed: " + currentSpeed);
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Jump logic
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Apply movement force with increased inertia for slipperiness
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        // Smooth rotation without snapping
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f); // Reduced rotation speed for smoother turns
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // Reset y velocity before jumping
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Apply jump force
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        // Trigger jump animation
        if (_animator != null)
        {
            _animator.SetTrigger("Jump");
        }

        Debug.Log("Player jumped!"); // Debug to confirm jump logic
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water")) // Ensure water colliders are tagged as "Water"
        {
            // Call the Game Over UI
            GameUI.Instance.ShowGameOver();

            // Optionally, disable player controls or handle respawn
            // For example:
            // this.enabled = false;
            // Or reload the scene after a delay
            // Invoke(nameof(ReloadScene), 2f);
        }
        if (other.CompareTag("CampFire"))
        {
            Debug.Log("Player reached the camp fire. Congratulations!");
            // Call the Congrats method from GameUI
            if (GameUI.Instance != null)
            {
                GameUI.Instance.ShowCongrats();
            }
            else
            {
                Debug.LogError("GameUI Instance is not set. Ensure the GameUI script is properly initialized.");
            }

            // Optional: Handle game completion, such as stopping player movement or transitioning to a new scene
            // Example: Disable player controls
            this.enabled = false;
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (playerHeight * 0.5f + groundDistance));
    }
}
