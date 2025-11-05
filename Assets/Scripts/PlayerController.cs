using UnityEngine;
using UnityEngine.InputSystem;

public enum SurfaceType
{
    Dirt,
    Cave
}

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float airControlMultiplier = 0.3f;
    
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;
    public float exhaustionCooldown = 3f;
    
    [Header("Mouse Look")]
    public float mouseSensitivityX = .5f;
    public float mouseSensitivityY = .5f;
    public float verticalLookLimit = 80f;
    public Transform headTransform;
    
    [Header("Field of View")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovTransitionSpeed = 8f;
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation = 0f;
    private Camera playerCamera;

    // action callback states
    private bool scanPressed;
    private bool sprintPressed;
    
    // Stamina system
    private float currentStamina = 100f;
    private bool isExhausted = false;
    private float exhaustionFinishes = 0f;

    private bool wantsToCrouch = false;
    private bool isCrouching = false;
    
    private float standHeight = 2f;
    private float crouchHeight = 1f;
    
    private Vector2 currentSway = Vector2.zero;
    private Vector2 targetSway = Vector2.zero;
    private float bobTimer = 0f;
    
    private Vector3 airMomentum = Vector3.zero;
    private bool wasGroundedLastFrame = true;
    private bool wasSprintingWhenAirborne = false;
    
    private float footstepTimer = 0f;
    private float footstepInterval = 0.7f; 
    private bool wasMovingLastFrame = false;
    
    private CharacterController characterController;
    private Vector3 velocity;
    
    [Header("Ground Detection")]
    public float coyoteTime = 0.1f;
    private float lastGroundedTime;
    public float slopeForce = 8f;
    public float slopeForceRayLength = 2f;
    
    [Header("Gauntlet")]
    public Gauntlet gauntlet;
    
    [Header("Weapon Transforms")]
    public Transform weaponPivot;
    public Transform weaponHolder;
    
    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;
    public float swaySpeed = 6f;
    public float swayResetSpeed = 8f;
    public Vector2 swayClamp = new Vector2(0.1f, 0.08f);
    
    [Header("Weapon Bob")]
    public float bobSpeed = 10f;
    public float bobAmount = 0.05f;
    public float bobSideAmount = 0.02f;
    
    [Header("Audio")]
    public AudioClip[] dirtFootstepSounds = new AudioClip[3];
    public AudioClip[] caveFootstepSounds = new AudioClip[3];
    public AudioClip gauntletScanSound;
    public AudioSource footstepAudioSource;
    public AudioSource gauntletAudioSource;
    public float footstepVolume = 0.5f;
    public float gauntletVolume = 0.8f;
    
    [Header("Noise System")]
    public float baseNoiseRadius = 2f;
    public float movementNoiseMultiplier = 1.5f;
    public float jumpNoiseMultiplier = 3f;
    public float crouchNoiseMultiplier = 0.5f;
    
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 5f;
    
    [Header("Inventory")]
    public GameObject[] inventory = new GameObject[3];
    public int selectedSlot = 0;
    public float interactionRange = 3f;
    


    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
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

        }
    }

    public void OnJump(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            bool isNearGround = Physics.Raycast(rayOrigin, Vector3.down, out hit, 2.0f);
            bool isGroundClose = isNearGround && hit.distance <= 1.3f;
            
            bool canJump = isGroundClose || (Time.time - lastGroundedTime <= coyoteTime);
            
            if (canJump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                EmitNoise(jumpNoiseMultiplier);
            }
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

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            sprintPressed = true;
        }
        else if (context.canceled)
        {
            sprintPressed = false;
        }
    }

    void Update()
    {
        velocity.y += gravity * Time.deltaTime;
        
        // Ground detection
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool isNearGround = Physics.Raycast(rayOrigin, Vector3.down, out hit, 2.0f);
        
        // Track ground state for coyote time using raycast - be more forgiving
        if (isNearGround && hit.distance <= 1.3f)
        {
            lastGroundedTime = Time.time;
            if (velocity.y < 0)
            {
                velocity.y = 0f;
            }
        }
        
        HandleMouseLook();
        HandleStamina();
        HandleFOV();
        HandleWeaponSway();
        HandleMovement();
        HandleCrouch();
        HandleScan();
        HandleHealthRegen();
        HandleInteraction();
        HandleItemUse();
    }
    
    void HandleMovement()
    {

        RaycastHit groundHit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool isNearGround = Physics.Raycast(rayOrigin, Vector3.down, out groundHit, 2.0f);
        bool isGrounded = isNearGround && groundHit.distance <= 1.3f;
        
        // Track momentum and sprint state when leaving ground
        if (wasGroundedLastFrame && !isGrounded)
        {
            // Capture current horizontal velocity as air momentum
            Vector3 currentHorizontalVel = characterController.velocity;
            currentHorizontalVel.y = 0f; // Remove vertical component
            airMomentum = currentHorizontalVel;
            
            // Capture sprint state when leaving ground
            wasSprintingWhenAirborne = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
        }
        

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        Vector3 inputMove = (forward * moveInput.y + right * moveInput.x);
        inputMove = Vector3.ClampMagnitude(inputMove, 1f);
        
        Vector3 finalMove;
        
        if (isGrounded)
        {
            // Normal ground movement
            float currentSpeed = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f ? sprintSpeed : speed;
            finalMove = inputMove * currentSpeed;
            airMomentum = Vector3.zero; // Reset momentum when grounded
            wasSprintingWhenAirborne = false; // Reset airborne sprint state
        }
        else
        {
            // Air movement: preserve momentum but allow some control
            Vector3 airInput = inputMove * speed * airControlMultiplier;
            
            // Add input to momentum rather than replacing it
            finalMove = airMomentum + airInput;
            
            // Gradually reduce momentum over time for realistic air resistance
            airMomentum = Vector3.Lerp(airMomentum, Vector3.zero, 0.5f * Time.deltaTime);
        }
        
        wasGroundedLastFrame = isGrounded;
        

        if (isGrounded && Vector3.Angle(groundHit.normal, Vector3.up) > 0.1f)
        {

            float movementMultiplier = moveInput.magnitude > 0.1f ? 1.5f : 1.0f;
            float distanceMultiplier = groundHit.distance / 2.0f;
            velocity.y -= slopeForce * distanceMultiplier * movementMultiplier * Time.deltaTime;
        }
        
        Vector3 finalMovement = finalMove + (velocity.y * Vector3.up);
        characterController.Move(finalMovement * Time.deltaTime);
        

        HandleFootstepAudio(isGrounded, finalMove.magnitude);
    }
    
    void HandleStamina()
    {

        if (isExhausted && Time.time >= exhaustionFinishes)
        {
            isExhausted = false;
        }
        

        bool isSprinting = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
        
        if (isSprinting)
        {
            // Drain stamina while sprinting
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            

            if (currentStamina <= 0f)
            {
                isExhausted = true;
                exhaustionFinishes = Time.time + exhaustionCooldown;
            }
        }
        else
        {
            // Regenerate stamina when not sprinting
            if (!isExhausted)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }
        }
    }
    
    bool CanSprint()
    {
        return !isExhausted && currentStamina > 0f && !isCrouching;
    }
    
    void HandleMouseLook()
    {
        if (playerCamera == null || headTransform == null) 
        {
            Debug.LogWarning("Camera or head not found for mouse look!");
            return;
        }
        

        float mouseX = lookInput.x * mouseSensitivityX;
        float mouseY = lookInput.y * mouseSensitivityY;
        
        // Rotate player body left/right (Y axis)
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate head up/down (X axis) with clamping
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        headTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    void HandleFOV()
    {
        if (playerCamera == null) return;
        

        RaycastHit groundHit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool isNearGround = Physics.Raycast(rayOrigin, Vector3.down, out groundHit, 2.0f);
        bool isGrounded = isNearGround && groundHit.distance <= 1.3f;
        

        bool isSprinting;
        if (isGrounded)
        {

            isSprinting = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
        }
        else
        {
            // In air: maintain sprint FOV if you were sprinting when you left ground
            isSprinting = wasSprintingWhenAirborne;
        }
        
        float targetFOV = isSprinting ? sprintFOV : normalFOV;
        

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }
    
    void HandleWeaponSway()
    {
        if (weaponHolder == null) return;
        

        targetSway.x = -lookInput.x * swayAmount;
        targetSway.y = -lookInput.y * swayAmount;
        
        // Clamp sway to limits
        targetSway.x = Mathf.Clamp(targetSway.x, -swayClamp.x, swayClamp.x);
        targetSway.y = Mathf.Clamp(targetSway.y, -swayClamp.y, swayClamp.y);
        

        float swayLerpSpeed = lookInput.magnitude > 0.1f ? swaySpeed : swayResetSpeed;
        currentSway = Vector2.Lerp(currentSway, targetSway, swayLerpSpeed * Time.deltaTime);
        

        Vector3 bobOffset = Vector3.zero;
        if (moveInput.magnitude > 0.1f)
        {

            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            bool isNearGround = Physics.Raycast(rayOrigin, Vector3.down, 1.5f);
            
            if (isNearGround)
            {
                bobTimer += Time.deltaTime * bobSpeed;
                float bobY = Mathf.Sin(bobTimer) * bobAmount;
                float bobX = Mathf.Sin(bobTimer * 0.5f) * bobSideAmount;
                bobOffset = new Vector3(bobX, bobY, 0f);
            }
        }
        

        Vector3 finalPosition = new Vector3(currentSway.x, currentSway.y, 0f) + bobOffset;
        weaponHolder.localPosition = finalPosition;
        

        Vector3 swayRotation = new Vector3(currentSway.y, currentSway.x, -currentSway.x) * 2f;
        weaponHolder.localRotation = Quaternion.Euler(swayRotation);
    }

    void HandleCrouch() 
    {
        // Handle input intent vs actual crouching state separately
        
        // Player wants to crouch but isn't crouching yet
        if (wantsToCrouch && !isCrouching)
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
            PlayGauntletScanSound();
            scanPressed = false; // Reset after use
        }
    }
    
    void HandleFootstepAudio(bool isGrounded, float movementSpeed)
    {
        bool isMoving = isGrounded && movementSpeed > 0.1f;
        
        // If we stopped moving, reset the timer
        if (wasMovingLastFrame && !isMoving)
        {
            footstepTimer = 0f;
        }
        
        // No footsteps if not moving, not grounded, or no audio source
        if (!isMoving || footstepAudioSource == null)
        {
            wasMovingLastFrame = false;
            return;
        }
        
        // Calculate footstep interval based on movement state
        float currentFootstepInterval = footstepInterval;
        
        // Check if actually sprinting (same logic as movement system)
        bool isSprinting = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f && isGrounded;
        
        if (isSprinting)
        {
            currentFootstepInterval *= 0.6f; // Significantly faster when sprinting
        }
        else if (movementSpeed > speed * 0.7f) // Fast walking (but not sprinting)
        {
            currentFootstepInterval *= 0.8f; // Slightly faster than normal
        }
        
        if (isCrouching)
        {
            currentFootstepInterval *= 1.8f; // Much slower when crouching
        }
        
        footstepTimer += Time.deltaTime;
        
        if (footstepTimer >= currentFootstepInterval)
        {
            SurfaceType surfaceType = DetectSurfaceType();
            PlayRandomFootstep(surfaceType);
            footstepTimer = 0f;
        }
        
        wasMovingLastFrame = true;
    }
    
    void PlayRandomFootstep(SurfaceType surfaceType)
    {
        if (footstepAudioSource == null) return;
        
        AudioClip[] currentFootstepSounds = null;
        

        switch (surfaceType)
        {
            case SurfaceType.Dirt:
                currentFootstepSounds = dirtFootstepSounds;
                break;
            case SurfaceType.Cave:
                currentFootstepSounds = caveFootstepSounds;
                break;
        }
        
        if (currentFootstepSounds == null || currentFootstepSounds.Length == 0) return;
        

        int randomIndex = Random.Range(0, currentFootstepSounds.Length);
        AudioClip footstepClip = currentFootstepSounds[randomIndex];
        
        if (footstepClip != null)
        {
            footstepAudioSource.volume = footstepVolume;
            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
            footstepAudioSource.PlayOneShot(footstepClip);
        }
    }
    
    SurfaceType DetectSurfaceType()
    {
        // Raycast down to detect what surface we're standing on
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 2.0f))
        {
            // Check the tag of the surface we hit
            if (hit.collider.CompareTag("Cave"))
            {
                return SurfaceType.Cave;
            }
            else if (hit.collider.CompareTag("Dirt"))
            {
                return SurfaceType.Dirt;
            }
            
            // Check material name if tags aren't used
            if (hit.collider.material != null)
            {
                string materialName = hit.collider.material.name.ToLower();
                if (materialName.Contains("cave") || materialName.Contains("stone") || materialName.Contains("rock"))
                {
                    return SurfaceType.Cave;
                }
                else if (materialName.Contains("dirt") || materialName.Contains("ground") || materialName.Contains("soil"))
                {
                    return SurfaceType.Dirt;
                }
            }
            
            // Check GameObject name as fallback
            string objectName = hit.collider.gameObject.name.ToLower();
            if (objectName.Contains("cave") || objectName.Contains("stone") || objectName.Contains("rock"))
            {
                return SurfaceType.Cave;
            }
        }
        
        // Default to dirt if we can't determine the surface type
        return SurfaceType.Dirt;
    }
    
    void PlayGauntletScanSound()
    {
        if (gauntletScanSound != null && gauntletAudioSource != null)
        {
            gauntletAudioSource.volume = gauntletVolume;
            gauntletAudioSource.PlayOneShot(gauntletScanSound);
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
    
    // === STAMINA PROPERTIES FOR UI ===
    public float StaminaPercentage => currentStamina / maxStamina;
    public bool IsExhausted => isExhausted;
    public float ExhaustionTimeRemaining => isExhausted ? Mathf.Max(0f, exhaustionFinishes - Time.time) : 0f;
    public bool IsSprinting => CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
    
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
