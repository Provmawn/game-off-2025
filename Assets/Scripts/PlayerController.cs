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
    private bool crouchPressed;
    private bool scanPressed;

    private float standHeight = 2f;
    private float crouchHeight = 1f;
    private bool isCrouching;
    
    private CharacterController characterController;
    private Vector3 velocity;
    
    [Header("Camera")]
    public Transform cameraTarget; // Drag your CameraTarget here
    public float standCameraHeight = 1.6f;
    public float crouchCameraHeight = 0.8f;

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
        crouchPressed = context.performed;
        Debug.Log(crouchPressed);
    }

    public void OnJump(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
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
        //HandleJump();
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
        }
        jumpPressed = false;
    }

    void HandleCrouch() {
        if (crouchPressed && !isCrouching && characterController.isGrounded) {
            isCrouching = true;
            characterController.height = crouchHeight;
            characterController.center = new Vector3(0, crouchHeight / 2, 0);
            
            // Move camera target down
            if (cameraTarget != null) {
                Vector3 pos = cameraTarget.localPosition;
                pos.y = crouchCameraHeight;
                cameraTarget.localPosition = pos;
            }
        } 
        else if (crouchPressed && isCrouching) {
            if (!Physics.Raycast(transform.position, Vector3.up, standHeight)) {
                isCrouching = false;
                characterController.height = standHeight;
                characterController.center = new Vector3(0, standHeight / 2, 0);
                
                // Move camera target back up
                if (cameraTarget != null) {
                    Vector3 pos = cameraTarget.localPosition;
                    pos.y = standCameraHeight;
                    cameraTarget.localPosition = pos;
                }
            }
        }
    }
    
}
