using UnityEngine;

public class ItemHolder : MonoBehaviour
{
    Interactable interactable;

    public void PickUp(Interactable interactable)
    {
        this.interactable = interactable;
        interactable.isInteractable = false;
        interactable.HideInteractText();

        Rigidbody rb = interactable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        interactable.transform.SetParent(transform);
        interactable.transform.localPosition = Vector3.zero;
    }

    public void Throw(float force)
    {
        if (interactable == null)
        {
            return;
        }

        interactable.isInteractable = true;
        interactable.transform.SetParent(null);
        
        Rigidbody rb = interactable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("throwing");
            rb.isKinematic = false;
            rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
        }
        
        interactable = null;
    }
}
