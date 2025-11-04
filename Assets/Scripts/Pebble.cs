using UnityEngine;

public class Pebble : MonoBehaviour
{
    [Header("Item Properties")]
    public string itemName = "Pebble";
    public bool canBePickedUp = true;
    public bool canBeThrown = true;
    public float throwForce = 10f;
    public float noiseRadius = 5f; // Noise when hitting ground
    
    private Rigidbody rb;
    private bool hasBeenThrown = false;
    private ScanHighlight scanHighlight;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
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
            
            Debug.Log("Pebble thrown!");
            // TODO: Emit throw noise
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasBeenThrown && collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log($"Pebble hit ground - noise radius: {noiseRadius}");
            // TODO: Notify monsters of noise
            
            // Break the pebble
            BreakPebble();
        }
    }
    
    void BreakPebble()
    {
        Debug.Log("Pebble broke!");
        // TODO: Add break effect/sound
        Destroy(gameObject);
    }
}
