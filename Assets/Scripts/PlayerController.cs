using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    
    private Vector2 moveInput;
    private Vector2 lookInput;

    // action callback states
    private bool jumpPressed;
    private bool scanPressed;

    // Crouch system - separate input intent from actual state
    private bool wantsToCrouch = false;  // Player's input intent
    private bool isCrouching = false;    // Actual crouching state
    
    private float standHeight = 2f;
    private float crouchHeight = 1f;
    
    private CharacterController characterController;
    private Vector3 velocity;
    


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnMove(InputAction.CallbackContext context) 
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context) 
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnScan(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            scanPressed = true;
        }
        else if (context.canceled)
        {
            scanPressed = false;
        }
    }

    public void OnInteract(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
        }
    }

    public void OnCrouch(InputAction.CallbackContext context) 
    {
        if (context.performed) 
        {
            wantsToCrouch = !wantsToCrouch; // Toggle what the player wants to do
            Debug.Log($"Crouch button pressed - wantsToCrouch is now: {wantsToCrouch}");
        }
    }

    public void OnJump(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            Debug.Log("jump input detected");
            jumpPressed = true;
        }
    }

    public void OnInventory(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
        }
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCrouch();
    }
    


    void HandleMovement()
    {
        bool groundedPlayer = characterController.isGrounded;
        if (groundedPlayer && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        Camera mainCam = Camera.main;
        Vector3 cameraForward = mainCam.transform.forward;
        Vector3 cameraRight = mainCam.transform.right;
        
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 move = (cameraForward * moveInput.y + cameraRight * moveInput.x);
        move = Vector3.ClampMagnitude(move, 1f);
        
        velocity.y += gravity * Time.deltaTime;
        
        Vector3 finalMove = (move * speed) + (velocity.y * Vector3.up);
        characterController.Move(finalMove * Time.deltaTime);
    }
    
    void HandleJump()
    {
        if (jumpPressed && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("Jumped");
        }
        jumpPressed = false;
    }

    void HandleCrouch() 
    {
        // Handle input intent vs actual crouching state separately
        
        // Player wants to crouch but isn't crouching yet
        if (wantsToCrouch && !isCrouching && characterController.isGrounded)
        {
            isCrouching = true;
            characterController.center = new Vector3(0, crouchHeight / 2, 0);
            Debug.Log("Started crouching");
        } 
        // Player wants to stand but is currently crouching
        else if (!wantsToCrouch && isCrouching) 
        {

            // Check if there's room to stand up
            if (!Physics.Raycast(transform.position, Vector3.up, standHeight, LayerMask.GetMask("Obstacle"))) 
            {
                isCrouching = false;
                characterController.center = new Vector3(0, standHeight / 2, 0);
                Debug.Log("Stopped crouching");
            }
            else 
            {
                Debug.Log("Can't stand up - ceiling in the way!");
                // Reset input intent to match current state (stay crouched)
                wantsToCrouch = true;
            }
        }
    }
    
}
