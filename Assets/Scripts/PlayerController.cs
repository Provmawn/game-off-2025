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
    
    [Header("Ground Detection")]
    public float coyoteTime = 0.1f;
    public float groundSnappingDistance = 0.5f;
    public bool constrainVelocityToGroundPlane = true;
    
    [Header("Slope Handling")]
    public float slopeLimit = 85f;
    public bool preventGroundingWhenMovingTowardsNoGrounding = true;
    public bool hasMaxDownwardSlopeChangeAngle = true;
    public float maxDownwardSlopeChangeAngle = 60f;
    
    [Header("Component References")]
    public Gauntlet gauntlet;
    public Transform weaponPivot;
    public Transform weaponHolder;
    public Transform itemHolder;
    
    [Header("Item System")]
    public GameObject currentHeldItem;
    
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

    [Header("Interactor")]
    public float interactDistance = 3f;
    public LayerMask interactMask;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation = 0f;
    private Camera playerCamera;
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
    private float lastGroundedTime;
    private float lastLandSoundTime = 0f;
    private float landSoundCooldown = 0.5f;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        characterController.slopeLimit = slopeLimit;
        
        if (gauntlet == null) gauntlet = GetComponentInChildren<Gauntlet>();
        
        SetupAudioSources();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandlePhysics();
        HandleMouseLook();
        HandleStamina();
        HandleFOV();
        HandleWeaponSway();
        HandleMovement();
        HandleCrouch();
        HandleHealthRegen();
    }
    
    public void OnMove(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();
    public void OnScan(InputAction.CallbackContext context) { if (context.performed && gauntlet != null) { gauntlet.Scan(); } }

    public void OnInteract(InputAction.CallbackContext context) {
        if (context.started)
        {
            if (currentHeldItem != null)
            {
                DropHeldItem();
                return;
            }
            
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, interactDistance, interactMask))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null && interactable.isInteractable)
                {
                    currentHeldItem = interactable.gameObject;
                    
                    if (gauntlet != null && gauntlet.ItemHolder != null)
                    {
                        gauntlet.ItemHolder.PickUp(interactable);
                    }
                    else
                    {
                        interactable.Interact();
                    }
                }
            }
        }
    }

    public void OnCrouch(InputAction.CallbackContext context) { if (context.performed) wantsToCrouch = !wantsToCrouch; }
    public void OnJump(InputAction.CallbackContext context) { if (context.performed) TryJump(); }
    public void OnSprint(InputAction.CallbackContext context) { sprintPressed = context.started ? true : (context.canceled ? false : sprintPressed); }
    
    void DropHeldItem()
    {
        if (currentHeldItem == null) return;
        
        currentHeldItem.transform.SetParent(null);
        
        Vector3 dropPosition = transform.position + transform.forward * 2f + Vector3.up * 1f;
        currentHeldItem.transform.position = dropPosition;
        
        Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }
        
        Collider[] colliders = currentHeldItem.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        Interactable interactable = currentHeldItem.GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isInteractable = true;
        }
        
        currentHeldItem = null;
    }
    
    void HandlePhysics()
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
    }
    
    void TryJump()
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
    
    bool CanSprint() => !isExhausted && currentStamina > 0f && !isCrouching;
    
    void HandleMouseLook()
    {
        if (playerCamera == null || headTransform == null)
        {
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
        
        bool isSprinting = isGrounded ? 
            (CanSprint() && sprintPressed && moveInput.magnitude > 0.1f) : 
            wasSprintingWhenAirborne;
        
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
        }
        else if (!wantsToCrouch && isCrouching)
        {
            if (!Physics.Raycast(transform.position, Vector3.up, standHeight, LayerMask.GetMask("Obstacle")))
            {
                isCrouching = false;
                characterController.center = new Vector3(0, standHeight / 2, 0);
            }
            else
            {
                wantsToCrouch = true;
            }
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
        
        AudioClip[] currentFootstepSounds = surfaceType == SurfaceType.Cave ? 
            caveFootstepSounds : dirtFootstepSounds;
        
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
            if (hit.collider.CompareTag("Cave")) return SurfaceType.Cave;
            if (hit.collider.CompareTag("Dirt")) return SurfaceType.Dirt;
            
            if (hit.collider.material != null)
            {
                string materialName = hit.collider.material.name.ToLower();
                if (materialName.Contains("cave") || materialName.Contains("stone") || materialName.Contains("rock"))
                    return SurfaceType.Cave;
                if (materialName.Contains("dirt") || materialName.Contains("ground") || materialName.Contains("soil"))
                    return SurfaceType.Dirt;
            }
            
            string objectName = hit.collider.gameObject.name.ToLower();
            if (objectName.Contains("cave") || objectName.Contains("stone") || objectName.Contains("rock"))
                return SurfaceType.Cave;
        }
        
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
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f)
        {
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
    }
    
    void SetupAudioSources()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();
        
        if (footstepAudioSource == null)
        {
            footstepAudioSource = FindAudioSourceByName(audioSources, "FootstepAudio");
            if (footstepAudioSource == null)
            {
                GameObject footstepObj = new GameObject("FootstepAudio");
                footstepObj.transform.SetParent(transform);
                footstepAudioSource = footstepObj.AddComponent<AudioSource>();
                footstepAudioSource.playOnAwake = false;
                footstepAudioSource.spatialBlend = 0f;
            }
        }
        
        if (gauntletAudioSource == null)
        {
            gauntletAudioSource = FindAudioSourceByName(audioSources, "GauntletAudio");
            if (gauntletAudioSource == null)
            {
                GameObject gauntletObj = new GameObject("GauntletAudio");
                gauntletObj.transform.SetParent(transform);
                gauntletAudioSource = gauntletObj.AddComponent<AudioSource>();
                gauntletAudioSource.playOnAwake = false;
                gauntletAudioSource.spatialBlend = 0f;
            }
        }
        
        if (jumpLandAudioSource == null)
        {
            jumpLandAudioSource = FindAudioSourceByName(audioSources, "JumpLandAudio");
            if (jumpLandAudioSource == null)
            {
                GameObject jumpLandObj = new GameObject("JumpLandAudio");
                jumpLandObj.transform.SetParent(transform);
                jumpLandAudioSource = jumpLandObj.AddComponent<AudioSource>();
                jumpLandAudioSource.playOnAwake = false;
                jumpLandAudioSource.spatialBlend = 0f;
            }
        }
        
    }
    
    AudioSource FindAudioSourceByName(AudioSource[] sources, string name)
    {
        foreach (AudioSource source in sources)
        {
            if (source.gameObject.name == name)
                return source;
        }
        return null;
    }
    
    public float StaminaPercentage => currentStamina / maxStamina;
    public bool IsExhausted => isExhausted;
    public float ExhaustionTimeRemaining => isExhausted ? Mathf.Max(0f, exhaustionFinishes - Time.time) : 0f;
    public bool IsSprinting => CanSprint() && sprintPressed && moveInput.magnitude > 0.1f;
    
    
    
}