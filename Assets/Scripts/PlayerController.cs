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
    private bool scanPressed;
    private bool sprintPressed;
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
    private bool wasAirborneLastFrame = false;
    private bool isJumping = false;
    private float jumpTime = 0f;
    private CharacterController characterController;
    private Vector3 velocity;
    [Header("Ground Detection")]
    public float coyoteTime = 0.1f;
    private float lastGroundedTime;
    [Header("Ground Snapping (Unity's Solution)")]
    public float groundSnappingDistance = 0.5f; 
    public bool constrainVelocityToGroundPlane = true;
    [Header("Slope Handling")]
    public float slopeLimit = 85f; 
    public bool preventGroundingWhenMovingTowardsNoGrounding = true;
    public bool hasMaxDownwardSlopeChangeAngle = true;
    public float maxDownwardSlopeChangeAngle = 60f;
    private float lastLandSoundTime = 0f;
    private float landSoundCooldown = 0.5f;
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
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioSource footstepAudioSource;
    public AudioSource gauntletAudioSource;
    public AudioSource jumpLandAudioSource;
    public float footstepVolume = 0.5f;
    public float gauntletVolume = 0.8f;
    public float jumpLandVolume = 0.7f;
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
        characterController.slopeLimit = slopeLimit;
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
            Debug.Log("Scan input received - calling scan directly");
            if (gauntlet != null)
            {
                gauntlet.Scan();
                PlayGauntletScanSound();
            }
            else
            {
                Debug.Log("No gauntlet assigned!");
            }
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
            wantsToCrouch = !wantsToCrouch; 
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
                isJumping = true;
                jumpTime = Time.time;
                EmitNoise(jumpNoiseMultiplier);
                PlayJumpSound();
            }
        }
    }
    public void OnInventory(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            Debug.Log("Inventory opened");
        }
    }
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
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool isNearGround = Physics.Raycast(rayOrigin, Vector3.down, out hit, 2.0f);
        bool isGrounded = isNearGround && hit.distance <= 1.3f;
        if (isGrounded && preventGroundingWhenMovingTowardsNoGrounding && moveInput.magnitude > 0.1f)
        {
            Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            Vector3 futurePosition = transform.position + moveDirection * (speed * 0.5f);
            Vector3 futureRayOrigin = futurePosition + Vector3.up * 0.1f;
            if (!Physics.Raycast(futureRayOrigin, Vector3.down, 1.5f))
            {
                isGrounded = false;
            }
        }
        if (isGrounded && hasMaxDownwardSlopeChangeAngle && wasGroundedLastFrame)
        {
            if (hit.normal != Vector3.zero)
            {
                float currentSlopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                if (currentSlopeAngle > maxDownwardSlopeChangeAngle)
                {
                    isGrounded = false;
                }
            }
        }
        if (isGrounded)
        {
            if (wasAirborneLastFrame && !wasGroundedLastFrame && Time.time > lastLandSoundTime + landSoundCooldown)
            {
                PlayLandSound();
                lastLandSoundTime = Time.time;
            }
            lastGroundedTime = Time.time;
            if (isJumping && velocity.y <= 0)
            {
                isJumping = false;
            }
            if (velocity.y < 0)
            {
                velocity.y = velocity.y < -2f ? -2f : velocity.y;
            }
            else if (velocity.y > 0 && !isJumping)
            {
                velocity.y = 0f;
            }
            wasAirborneLastFrame = false;
        }
        else
        {
            if (!wasGroundedLastFrame)
            {
                wasAirborneLastFrame = true;
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
        if (wasGroundedLastFrame && !isGrounded)
        {
            Vector3 currentHorizontalVel = characterController.velocity;
            currentHorizontalVel.y = 0f; 
            airMomentum = currentHorizontalVel;
            wasSprintingWhenAirborne = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
        }
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 inputMove = (forward * moveInput.y + right * moveInput.x);
        inputMove = Vector3.ClampMagnitude(inputMove, 1f);
        Vector3 finalMove;
        if (isGrounded)
        {
            float currentSpeed = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f ? sprintSpeed : speed;
            finalMove = inputMove * currentSpeed;
            airMomentum = Vector3.zero; 
            wasSprintingWhenAirborne = false; 
            if (constrainVelocityToGroundPlane && groundHit.normal != Vector3.zero)
            {
                finalMove = Vector3.ProjectOnPlane(finalMove, groundHit.normal);
            }
        }
        else
        {
            Vector3 airInput = inputMove * speed * airControlMultiplier;
            finalMove = airMomentum + airInput;
            airMomentum = Vector3.Lerp(airMomentum, Vector3.zero, 0.5f * Time.deltaTime);
        }
        wasGroundedLastFrame = isGrounded;
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            Vector3 snapRayOrigin = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(snapRayOrigin, Vector3.down, out RaycastHit snapHit, groundSnappingDistance + 0.1f))
            {
                float distanceToGround = snapHit.distance - 0.1f;
                if (distanceToGround > 0.01f && distanceToGround <= groundSnappingDistance)
                {
                    Vector3 snapAmount = Vector3.down * (distanceToGround - 0.01f);
                    characterController.Move(snapAmount);
                }
            }
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
        transform.Rotate(Vector3.up * mouseX);
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
        if (wantsToCrouch && !isCrouching)
        {
            isCrouching = true;
            characterController.center = new Vector3(0, crouchHeight / 2, 0);
            Debug.Log("Started crouching");
        } 
        else if (!wantsToCrouch && isCrouching) 
        {
            if (!Physics.Raycast(transform.position, Vector3.up, standHeight, LayerMask.GetMask("Obstacle"))) 
            {
                isCrouching = false;
                characterController.center = new Vector3(0, standHeight / 2, 0);
                Debug.Log("Stopped crouching");
            }
            else 
            {
                Debug.Log("Can't stand up - ceiling in the way!");
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
            scanPressed = false;
        }
    }
    void HandleFootstepAudio(bool isGrounded, float movementSpeed)
    {
        bool isMoving = isGrounded && movementSpeed > 0.1f;
        if (wasMovingLastFrame && !isMoving)
        {
            footstepTimer = 0f;
        }
        if (!isMoving || footstepAudioSource == null)
        {
            wasMovingLastFrame = false;
            return;
        }
        float currentFootstepInterval = footstepInterval;
        bool isSprinting = CanSprint() && sprintPressed && moveInput.magnitude > 0.1f && isGrounded;
        if (isSprinting)
        {
            currentFootstepInterval *= 0.6f; 
        }
        else if (movementSpeed > speed * 0.7f) 
        {
            currentFootstepInterval *= 0.8f; 
        }
        if (isCrouching)
        {
            currentFootstepInterval *= 1.8f; 
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
            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f); 
            footstepAudioSource.PlayOneShot(footstepClip);
        }
    }
    SurfaceType DetectSurfaceType()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 2.0f))
        {
            if (hit.collider.CompareTag("Cave"))
            {
                return SurfaceType.Cave;
            }
            else if (hit.collider.CompareTag("Dirt"))
            {
                return SurfaceType.Dirt;
            }
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
            string objectName = hit.collider.gameObject.name.ToLower();
            if (objectName.Contains("cave") || objectName.Contains("stone") || objectName.Contains("rock"))
            {
                return SurfaceType.Cave;
            }
        }
        return SurfaceType.Dirt;
    }
    void PlayGauntletScanSound()
    {
        if (gauntletScanSound != null && gauntletAudioSource != null)
        {
            gauntletAudioSource.volume = gauntletVolume;
            gauntletAudioSource.PlayOneShot(gauntletScanSound);
            Debug.Log("Gauntlet scan sound played");
        }
        else
        {
            Debug.Log($"Scan sound failed - Sound: {gauntletScanSound != null}, AudioSource: {gauntletAudioSource != null}");
        }
    }
    void PlayJumpSound()
    {
        if (jumpSound != null && jumpLandAudioSource != null)
        {
            jumpLandAudioSource.volume = jumpLandVolume;
            jumpLandAudioSource.pitch = Random.Range(0.95f, 1.05f);
            jumpLandAudioSource.PlayOneShot(jumpSound);
        }
    }
    void PlayLandSound()
    {
        if (landSound != null && jumpLandAudioSource != null)
        {
            jumpLandAudioSource.volume = jumpLandVolume;
            jumpLandAudioSource.pitch = Random.Range(0.95f, 1.05f);
            jumpLandAudioSource.PlayOneShot(landSound);
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
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f)
        {
            Debug.Log("Player died!");
        }
    }
    public float GetCurrentNoiseRadius()
    {
        float noiseRadius = baseNoiseRadius;
        if (moveInput.magnitude > 0.1f)
        {
            noiseRadius *= movementNoiseMultiplier;
        }
        if (isCrouching)
        {
            noiseRadius *= crouchNoiseMultiplier;
        }
        return noiseRadius;
    }
    public void EmitNoise(float radiusMultiplier = 1f)
    {
        float noiseRadius = baseNoiseRadius * radiusMultiplier;
        Debug.Log($"Emitted noise with radius: {noiseRadius}");
    }
    public float StaminaPercentage => currentStamina / maxStamina;
    public bool IsExhausted => isExhausted;
    public float ExhaustionTimeRemaining => isExhausted ? Mathf.Max(0f, exhaustionFinishes - Time.time) : 0f;
    public bool IsSprinting => CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
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
    }
    void HandleInteractAction()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider obj in nearbyObjects)
        {
            if (obj.GetComponent<Pebble>() || obj.GetComponent<Glowstick>() || obj.GetComponent<PlantGel>())
            {
                TryPickupItem(obj.gameObject);
                break; 
            }
        }
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
        if (Input.GetMouseButtonDown(1) && inventory[selectedSlot] != null)
        {
            ThrowItem(selectedSlot);
        }
    }
    bool TryPickupItem(GameObject item)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = item;
                item.SetActive(false); 
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
                inventory[slot] = null; 
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
            Vector3 throwPosition = transform.position + transform.forward * 1f + Vector3.up * 1.5f;
            item.transform.position = throwPosition;
            item.SetActive(true); 
            Camera cam = Camera.main;
            Vector3 throwDirection = cam.transform.forward;
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
            inventory[slot] = null;
            EmitNoise(1.5f); 
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
