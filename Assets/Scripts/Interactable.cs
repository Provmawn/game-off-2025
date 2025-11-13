using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string itemName;
    public bool isInteractable = true;
    public GameObject interactText;

    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected on interactable");
            ShowInteractText();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HideInteractText();
        }
    }

    public virtual void Interact()
    {
        Debug.Log("interacting");
        interactText.SetActive(false);
        isInteractable = false;
    }

    public void ShowInteractText()
    {
        interactText.SetActive(true);
    }

    public void HideInteractText()
    {
        interactText.SetActive(false);
    }

    public void EnableInteractable()
    {
        isInteractable = false;
    }

    public void DisableInteractable()
    {
        isInteractable = true;
    }

}
