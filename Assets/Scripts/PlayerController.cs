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
    private bool jumpPressed;
    private bool crouchPressed;
    private bool scanPressed;
    
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
        Debug.Log("Trying to move");
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
        Debug.Log($"Crouch: {crouchPressed}");
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
        HandleJump();
    }
    
    void HandleMovement()
    {
        bool isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        Camera mainCam = Camera.main;
        Vector3 forward = mainCam.transform.forward;
        Vector3 right = mainCam.transform.right;
        
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x) * speed;
        
        velocity.y += gravity * Time.deltaTime;
        
        Vector3 finalMovement = moveDirection * Time.deltaTime + velocity * Time.deltaTime;
        
        characterController.Move(finalMovement);
    }
    
    void HandleJump()
    {
        if (jumpPressed && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        jumpPressed = false;
    }
    
}
