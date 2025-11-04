using UnityEngine;

public class Glowstick : MonoBehaviour
{
    [Header("Item Properties")]
    public string itemName = "Glowstick";
    public bool canBePickedUp = true;
    public bool canBeThrown = true;
    public float throwForce = 8f;
    public float noiseRadius = 3f;
    
    [Header("Light Properties")]
    public float lightDuration = 30f; // 30 seconds
    public Light glowLight;
    public Color glowColor = Color.green;
    public float lightIntensity = 2f;
    
    private Rigidbody rb;
    private bool hasBeenThrown = false;
    private bool isActivated = false;
    private float activationTime;
    private ScanHighlight scanHighlight;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Set up light component
        if (glowLight == null)
        {
            glowLight = gameObject.AddComponent<Light>();
        }
        glowLight.color = glowColor;
        glowLight.intensity = 0f; // Start dim
        glowLight.range = 10f;
        
        scanHighlight = GetComponent<ScanHighlight>();
        if (scanHighlight == null)
        {
            scanHighlight = gameObject.AddComponent<ScanHighlight>();
        }
    }
    
    public void OnScanned()
    {
        Debug.Log($"{itemName} was scanned!");
        scanHighlight.StartHighlight();
    }
    
    void Update()
    {
        if (isActivated)
        {
            float timeElapsed = Time.time - activationTime;
            float timeRemaining = lightDuration - timeElapsed;
            
            if (timeRemaining <= 0f)
            {
                // Light expired
                glowLight.intensity = 0f;
                Debug.Log("Glowstick light expired");
            }
            else
            {
                // Fade light over time
                float fadePercent = timeRemaining / lightDuration;
                glowLight.intensity = lightIntensity * fadePercent;
            }
        }
    }
    
    public void PickUp()
    {
        if (canBePickedUp)
        {
            Debug.Log($"Picked up {itemName}");
            // TODO: Add to player inventory
            gameObject.SetActive(false);
        }
    }
    
    public void Throw(Vector3 direction, float force = -1f)
    {
        if (canBeThrown && rb != null)
        {
            if (force < 0) force = throwForce;
            
            hasBeenThrown = true;
            rb.isKinematic = false;
            rb.AddForce(direction * force, ForceMode.Impulse);
            
            // Activate light when thrown
            ActivateLight();
            
            Debug.Log("Glowstick thrown and activated!");
            // TODO: Emit throw noise
        }
    }
    
    void ActivateLight()
    {
        if (!isActivated)
        {
            isActivated = true;
            activationTime = Time.time;
            glowLight.intensity = lightIntensity;
            Debug.Log("Glowstick light activated!");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasBeenThrown && collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log($"Glowstick hit ground - noise radius: {noiseRadius}");
            // TODO: Notify monsters of noise
        }
    }
}
