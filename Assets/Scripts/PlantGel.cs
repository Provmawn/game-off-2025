using UnityEngine;

public class PlantGel : MonoBehaviour, IScannable, IPickupable
{
    [Header("Item Properties")]
    public string itemName = "Plant Gel";
    public bool canBePickedUp = true;
    public bool canBeConsumed = true;
    
    [Header("Consumption Effects")]
    public float healthRestore = 50f;
    public float consumeTime = 2f;
    
    private ScanHighlight scanHighlight;
    
    void Start()
    {
        scanHighlight = GetComponent<ScanHighlight>();
        if (scanHighlight == null)
        {
            scanHighlight = gameObject.AddComponent<ScanHighlight>();
        }
    }
    
    public void PickUp()
    {
        if (canBePickedUp)
        {
            gameObject.SetActive(false);
        }
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
    
    public void Consume(PlayerController player)
    {
        if (canBeConsumed && player != null)
        {
            
            player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + healthRestore);
            
            
            Destroy(gameObject);
        }
    }
    
    public void OnScanned()
    {
        scanHighlight?.StartHighlight();
        GetComponent<OutlineHighlight>()?.StartHighlight();
    }
    
    public string GetScanInfo()
    {
        return $"{itemName} - Restores {healthRestore} health";
    }
    
    public ScanType GetScanType()
    {
        return ScanType.Item;
    }
}
