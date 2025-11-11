using UnityEngine;

public class Glowstick : MonoBehaviour, IPickupable
{
    [Header("Item Properties")]
    public string itemName = "Glowstick";
    public bool canBePickedUp = true;
    public bool canBeThrown = true;
    public float throwForce = 8f;
    public float noiseRadius = 3f;
    
    [Header("Light Properties")]
    public float lightDuration = 30f;
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
        
        if (glowLight == null)
        {
            glowLight = gameObject.AddComponent<Light>();
        }
        glowLight.color = glowColor;
        glowLight.intensity = 0f;
        glowLight.range = 10f;
        
        scanHighlight = GetComponent<ScanHighlight>();
        if (scanHighlight == null)
        {
            scanHighlight = gameObject.AddComponent<ScanHighlight>();
        }
    }
    
    public void OnScanned()
    {
        scanHighlight.StartHighlight();
    }
    
    public string ItemName => itemName;
    public bool CanBePickedUp => canBePickedUp;
    
    public void PickUp(PlayerController player)
    {
        if (canBePickedUp)
        {
            gameObject.SetActive(false);
        }
    }
    
    public GameObject GetGameObject()
    {
        return gameObject;
    }
    
    void Update()
    {
        if (isActivated)
        {
            float timeElapsed = Time.time - activationTime;
            float timeRemaining = lightDuration - timeElapsed;
            
            if (timeRemaining <= 0f)
            {
                glowLight.intensity = 0f;
            }
            else
            {
                float fadePercent = timeRemaining / lightDuration;
                glowLight.intensity = lightIntensity * fadePercent;
            }
        }
    }
    
    public void PickUp()
    {
        if (canBePickedUp)
        {
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
            
            ActivateLight();
            
        }
    }
    
    void ActivateLight()
    {
        if (!isActivated)
        {
            isActivated = true;
            activationTime = Time.time;
            glowLight.intensity = lightIntensity;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasBeenThrown && collision.gameObject.CompareTag("Ground"))
        {
        }
    }
}
