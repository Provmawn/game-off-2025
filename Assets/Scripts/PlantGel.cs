using UnityEngine;

public class PlantGel : MonoBehaviour
{
    [Header("Item Properties")]
    public string itemName = "Plant Gel";
    public bool canBePickedUp = true;
    public bool canBeConsumed = true;
    
    [Header("Consumption Effects")]
    public float healthRestore = 50f;
    public float consumeTime = 2f; // Time to consume
    
    private ScanHighlight scanHighlight;
    
    void Start()
    {
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
    
    public void Consume(PlayerController player)
    {
        if (canBeConsumed && player != null)
        {
            Debug.Log($"Consuming {itemName}...");
            
            // Restore health
            player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + healthRestore);
            
            Debug.Log($"Consumed {itemName}! Restored {healthRestore} health.");
            
            // Remove from inventory and destroy
            Destroy(gameObject);
        }
    }
}
