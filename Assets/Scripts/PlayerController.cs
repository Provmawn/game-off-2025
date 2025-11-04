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
    
    [Header("Gauntlet")]
    public Gauntlet gauntlet; // Drag your gauntlet here
    
    [Header("Noise System")]
    public float baseNoiseRadius = 2f;
    public float movementNoiseMultiplier = 1.5f;
    public float jumpNoiseMultiplier = 3f;
    public float crouchNoiseMultiplier = 0.5f;
    
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 5f; // per second
    
    [Header("Inventory")]
    public GameObject[] inventory = new GameObject[3]; // 3 slots max
    public int selectedSlot = 0; // Currently selected slot (0, 1, 2)
    public float interactionRange = 3f; // Range to pick up items
    


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
    }

    public void OnInteract(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            HandleInteractAction();
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
            // Toggle inventory UI (implement later)
            Debug.Log("Inventory opened");
        }
    }
    
    // New input methods for inventory
    public void OnItemSlotOne(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(0);
    }
    
    public void OnItemSlotTwo(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(1);
    }
    
    public void OnItemSlotThree(InputAction.CallbackContext context)
    {
        if (context.performed) SelectSlot(2);
    }
    
    public void OnScrollUp(InputAction.CallbackContext context)
    {
        if (context.performed) CycleInventory(1);
    }
    
    public void OnScrollDown(InputAction.CallbackContext context)
    {
        if (context.performed) CycleInventory(-1);
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleScan();
        HandleHealthRegen();
        HandleInteraction();
        HandleItemUse();
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
            EmitNoise(jumpNoiseMultiplier);
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
    
    void HandleScan()
    {
        if (scanPressed && gauntlet != null)
        {
            gauntlet.Scan();
            scanPressed = false; // Reset after use
        }
    }
    
    void HandleHealthRegen()
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
    }
    
    // Method for other scripts to damage the player
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        if (currentHealth <= 0f)
        {
            Debug.Log("Player died!");
            // Handle death here
        }
    }
    
    // Method to calculate current noise radius
    public float GetCurrentNoiseRadius()
    {
        float noiseRadius = baseNoiseRadius;
        
        // Check if moving
        if (moveInput.magnitude > 0.1f)
        {
            noiseRadius *= movementNoiseMultiplier;
        }
        
        // Apply crouch modifier
        if (isCrouching)
        {
            noiseRadius *= crouchNoiseMultiplier;
        }
        
        return noiseRadius;
    }
    
    // Method to emit noise (for jump, throwing items, etc.)
    public void EmitNoise(float radiusMultiplier = 1f)
    {
        float noiseRadius = baseNoiseRadius * radiusMultiplier;
        Debug.Log($"Emitted noise with radius: {noiseRadius}");
        // TODO: Notify nearby monsters of noise at this position
    }
    
    // === INVENTORY SYSTEM ===
    
    void SelectSlot(int slot)
    {
        if (slot >= 0 && slot < inventory.Length)
        {
            selectedSlot = slot;
            Debug.Log($"Selected inventory slot {slot + 1}");
            if (inventory[slot] != null)
            {
                Debug.Log($"Selected item: {GetItemName(inventory[slot])}");
            }
        }
    }
    
    void CycleInventory(int direction)
    {
        selectedSlot = (selectedSlot + direction) % inventory.Length;
        if (selectedSlot < 0) selectedSlot = inventory.Length - 1;
        
        Debug.Log($"Cycled to slot {selectedSlot + 1}");
        if (inventory[selectedSlot] != null)
        {
            Debug.Log($"Selected item: {GetItemName(inventory[selectedSlot])}");
        }
    }
    
    void HandleInteraction()
    {
        // This method is called every frame, no input checking here
        // Input is handled by OnInteract() method
    }
    
    void HandleInteractAction()
    {
        // Look for nearby items to pick up
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        
        foreach (Collider obj in nearbyObjects)
        {
            // Check for items we can pick up
            if (obj.GetComponent<Pebble>() || obj.GetComponent<Glowstick>() || obj.GetComponent<PlantGel>())
            {
                TryPickupItem(obj.gameObject);
                break; // Only pick up one item at a time
            }
        }
        
        // Check if we're trying to consume the selected item
        if (inventory[selectedSlot] != null)
        {
            PlantGel plantGel = inventory[selectedSlot].GetComponent<PlantGel>();
            if (plantGel != null)
            {
                ConsumeItem(selectedSlot);
            }
        }
    }
    
    void HandleItemUse()
    {
        // Right-click to throw selected item
        if (Input.GetMouseButtonDown(1) && inventory[selectedSlot] != null)
        {
            ThrowItem(selectedSlot);
        }
    }
    
    bool TryPickupItem(GameObject item)
    {
        // Find first empty slot
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = item;
                item.SetActive(false); // Hide the item in world
                Debug.Log($"Picked up {GetItemName(item)} in slot {i + 1}");
                return true;
            }
        }
        
        Debug.Log("Inventory full! Cannot pick up item.");
        return false;
    }
    
    void ConsumeItem(int slot)
    {
        if (inventory[slot] != null)
        {
            PlantGel plantGel = inventory[slot].GetComponent<PlantGel>();
            if (plantGel != null)
            {
                plantGel.Consume(this);
                inventory[slot] = null; // Remove from inventory
                Debug.Log($"Consumed item from slot {slot + 1}");
            }
        }
    }
    
    void ThrowItem(int slot)
    {
        if (inventory[slot] != null)
        {
            GameObject item = inventory[slot];
            string itemName = GetItemName(item);
            
            // Position item in front of player
            Vector3 throwPosition = transform.position + transform.forward * 1f + Vector3.up * 1.5f;
            item.transform.position = throwPosition;
            item.SetActive(true); // Make visible in world
            
            // Get throw direction (camera forward)
            Camera cam = Camera.main;
            Vector3 throwDirection = cam.transform.forward;
            
            // Throw the item based on its type
            Pebble pebble = item.GetComponent<Pebble>();
            Glowstick glowstick = item.GetComponent<Glowstick>();
            
            if (pebble != null)
            {
                pebble.Throw(throwDirection);
            }
            else if (glowstick != null)
            {
                glowstick.Throw(throwDirection);
            }
            
            // Remove from inventory
            inventory[slot] = null;
            EmitNoise(1.5f); // Throwing makes noise
            
            Debug.Log($"Threw {itemName} from slot {slot + 1}");
        }
    }
    
    string GetItemName(GameObject item)
    {
        Pebble pebble = item.GetComponent<Pebble>();
        if (pebble != null) return pebble.itemName;
        
        Glowstick glowstick = item.GetComponent<Glowstick>();
        if (glowstick != null) return glowstick.itemName;
        
        PlantGel plantGel = item.GetComponent<PlantGel>();
        if (plantGel != null) return plantGel.itemName;
        
        return "Unknown Item";
    }
    
    // Public methods for UI or other systems
    public bool IsInventoryFull()
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null) return false;
        }
        return true;
    }
    
    public GameObject GetSelectedItem()
    {
        return inventory[selectedSlot];
    }
    
}
