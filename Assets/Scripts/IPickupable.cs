using UnityEngine;

public interface IPickupable
{
    string ItemName { get; }
    bool CanBePickedUp { get; }
    void PickUp(PlayerController player);
    GameObject GetGameObject();
}