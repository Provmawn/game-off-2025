using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    [Header("Item Handling")]
    public GameObject currentHeldItem;
    public Transform itemHoldPosition;
    public float interactionRange = 3f;
    public LayerMask interactionLayers = -1;
    
    [Header("Detection")]
    public Transform cameraTransform;
    
    private IPickupable currentPickupable;
    private GameObject currentInteractable;
    
    void Update()
    {
        CheckForInteractables();
    }
    
    void CheckForInteractables()
    {
        IPickupable previousPickupable = currentPickupable;
        currentPickupable = null;
        currentInteractable = null;
        
        Camera cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayers))
            {
                IPickupable pickupable = hit.collider.GetComponent<IPickupable>();
                if (pickupable != null && pickupable.CanBePickedUp)
                {
                    currentPickupable = pickupable;
                    currentInteractable = hit.collider.gameObject;
                }
            }
        }
        
        if (currentPickupable == null)
        {
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange, interactionLayers);
            float closestDistance = interactionRange + 1;
            
            foreach (Collider obj in nearbyObjects)
            {
                IPickupable pickupable = obj.GetComponent<IPickupable>();
                if (pickupable != null && pickupable.CanBePickedUp)
                {
                    float distance = Vector3.Distance(transform.position, obj.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentPickupable = pickupable;
                        currentInteractable = obj.gameObject;
                    }
                }
            }
        }
        
        if (currentPickupable != previousPickupable)
        {
            if (currentPickupable != null)
            {
                if (currentHeldItem != null)
                {
                }
                else
                {
                }
            }
        }
    }
    
    public void Interact()
    {
        if (currentHeldItem != null)
        {
            DropHeldItem();
            return;
        }
        
        if (currentPickupable != null)
        {
            PickupItemInHand(currentPickupable.GetGameObject());
            return;
        }
        
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (Collider obj in nearbyObjects)
        {
            if (obj.GetComponent<Pebble>() || obj.GetComponent<Glowstick>() || obj.GetComponent<PlantGel>())
            {
                PickupItemInHand(obj.gameObject);
                break;
            }
        }
    }
    
    public void UseItem()
    {
        if (currentHeldItem == null) return;
        
        PlantGel plantGel = currentHeldItem.GetComponent<PlantGel>();
        if (plantGel != null && plantGel.canBeConsumed)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                plantGel.Consume(player);
                Destroy(currentHeldItem);
                currentHeldItem = null;
            }
        }
        else
        {
        }
    }
    
    public void ThrowItem()
    {
        if (currentHeldItem == null) return;
        
        string itemName = GetItemName(currentHeldItem);
        GameObject item = currentHeldItem;
        
        item.transform.SetParent(null);
        
        Vector3 throwPosition = transform.position + transform.forward * 1f + Vector3.up * 1.5f;
        item.transform.position = throwPosition;
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        Collider[] colliders = item.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        Camera cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;
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
        else if (rb != null)
        {
            rb.AddForce(throwDirection * 10f, ForceMode.Impulse);
        }
        
        currentHeldItem = null;
        
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.EmitNoise(1.5f);
        }
    }
    
    void PickupItemInHand(GameObject item)
    {
        if (currentHeldItem != null)
        {
            return;
        }
        
        string itemName = GetItemName(item);
        
        currentHeldItem = item;
        
        if (itemHoldPosition != null)
        {
            item.transform.SetParent(itemHoldPosition);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        Collider[] colliders = item.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        item.SetActive(true);
    }
    
    void DropHeldItem()
    {
        if (currentHeldItem == null) return;
        
        string itemName = GetItemName(currentHeldItem);
        
        currentHeldItem.transform.SetParent(null);
        
        Vector3 dropPosition = transform.position + transform.forward * 1.5f + Vector3.up * 1f;
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
        
        currentHeldItem = null;
    }
    
    public string GetItemName(GameObject item)
    {
        Pebble pebble = item.GetComponent<Pebble>();
        if (pebble != null) return pebble.itemName;
        
        Glowstick glowstick = item.GetComponent<Glowstick>();
        if (glowstick != null) return glowstick.itemName;
        
        PlantGel plantGel = item.GetComponent<PlantGel>();
        if (plantGel != null) return plantGel.itemName;
        
        return "Unknown Item";
    }
    
    public IPickupable GetCurrentPickupable() => currentPickupable;
    public bool IsHandsFull() => currentHeldItem != null;
    public GameObject GetHeldItem() => currentHeldItem;
}