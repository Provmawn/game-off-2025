using UnityEngine;

public class Pebble : MonoBehaviour, IPickupable
{
    [Header("Item Properties")]
    public string itemName = "Pebble";
    public bool canBePickedUp = true;
    public bool canBeThrown = true;
    public float throwForce = 10f;
    public float noiseRadius = 5f;
    
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
            
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasBeenThrown && collision.gameObject.CompareTag("Ground"))
        {
            
            BreakPebble();
        }
    }
    
    void BreakPebble()
    {
        Destroy(gameObject);
    }
}
