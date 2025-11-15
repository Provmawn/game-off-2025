using UnityEngine;

public class RadiationZone : MonoBehaviour
{
    public float nextRadiationTime = 0f;
    public float radiationInterval = 1f; 
    public float radiationAmount = 10f;
    
    void OnTriggerStay(Collider other)
    {
        if (Time.time >= nextRadiationTime)
        {
            nextRadiationTime = Time.time + radiationInterval;
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.IncreaseRadiation(10f);
                player.ScreenShake();
            }
        }
    }
}
