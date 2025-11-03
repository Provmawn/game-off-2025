 using UnityEngine;

public class Gauntlet : MonoBehaviour
{
    float maxHeat = 100f;
    float currentHeat = 0f;
    bool isOverheated = false;
    float overheatDuration = 5f;
    float overheatFinishes = 0f;
    float heatdecayRate = 1f;
    private void Update()
    {
        if (isOverheated)
        {
            if (Time.time >= overheatFinishes)
            {
                isOverheated = false;
                currentHeat = 0;
            }
        }
        else
        {
            currentHeat -= heatdecayRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);
        }
    }
    public void Scan()
    {
        if (currentHeat < maxHeat)
        {
            currentHeat += 20;
            Physics.SphereCastAll(transform.position, 5f, transform.forward, (1 << 7) | (1 << 8));
            if (currentHeat >= maxHeat)
            {
                Overheat();
            }
        }

    }
    
    public void Overheat()
    {
        isOverheated = true;
        overheatFinishes = Time.time + overheatDuration;
    }
}
