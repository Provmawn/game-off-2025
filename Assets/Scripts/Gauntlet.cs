 using UnityEngine;

public class Gauntlet : MonoBehaviour
{
    [Header("Heat System")]
    float maxHeat = 100f;
    float currentHeat = 0f;
    bool isOverheated = false;
    float overheatDuration = 10f; // 10 seconds as requested
    float overheatFinishes = 0f;
    float heatdecayRate = 1f;
    
    [Header("Scanning")]
    public float scanCooldown = 5f; // 5 second cooldown
    float lastScanTime = 0f;
    public float scanRange = 20f; // Increased range
    public float scanConeAngle = 90f; // Wider cone for testing
    
    [Header("Debug Visualization")]
    public bool showScanCone = true;
    public Color scanConeColor = Color.green;
    public int coneResolution = 20;
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
        if (Time.time < lastScanTime + scanCooldown)
        {
            return;
        }
        
        if (isOverheated)
        {
            return;
        }
        
        lastScanTime = Time.time;
        currentHeat += 20;
        
        LayerMask scanLayers = (1 << 0) | (1 << 7) | (1 << 8);
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRange, scanLayers);
        
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player") || hit.name.Contains("Player"))
            {
                continue;
            }
            
            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angleToTarget <= scanConeAngle / 2f)
            {
                if (hit.gameObject.layer == 8)
                {
                    
                }
                else if (hit.gameObject.layer == 7)
                {
                    Pebble pebble = hit.GetComponent<Pebble>();
                    Glowstick glowstick = hit.GetComponent<Glowstick>();
                    PlantGel plantGel = hit.GetComponent<PlantGel>();
                    
                    if (pebble != null)
                    {
                        pebble.OnScanned();
                    }
                    else if (glowstick != null)
                    {
                        glowstick.OnScanned();
                    }
                    else if (plantGel != null)
                    {
                        plantGel.OnScanned();
                    }
                }
            }
        }
        
        if (currentHeat >= maxHeat)
        {
            Overheat();
        }
    }
    
    public void Overheat()
    {
        isOverheated = true;
        overheatFinishes = Time.time + overheatDuration;
    }
    
    public float HeatPercentage => currentHeat / maxHeat;
    public bool IsOverheated => isOverheated;
    public float OverheatTimeRemaining => isOverheated ? Mathf.Max(0f, overheatFinishes - Time.time) : 0f;
    public float ScanCooldownRemaining => Mathf.Max(0f, (lastScanTime + scanCooldown) - Time.time);
    public bool CanScan => !isOverheated && Time.time >= lastScanTime + scanCooldown;
    
    void OnDrawGizmos()
    {
        if (showScanCone)
        {
            DrawScanCone();
        }
    }
    
    void DrawScanCone()
    {
        Gizmos.color = scanConeColor;
        
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        
        float halfAngle = scanConeAngle * 0.5f;
        
        for (int i = 0; i < coneResolution; i++)
        {
            float angle1 = (i / (float)coneResolution) * 360f;
            float angle2 = ((i + 1) / (float)coneResolution) * 360f;
            
            Vector3 direction1 = GetConeDirection(forward, right, up, halfAngle, angle1);
            Vector3 direction2 = GetConeDirection(forward, right, up, halfAngle, angle2);
            
            Vector3 point1 = transform.position + direction1 * scanRange;
            Vector3 point2 = transform.position + direction2 * scanRange;
            
            Gizmos.DrawLine(transform.position, point1);
            
            Gizmos.DrawLine(point1, point2);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, forward * scanRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + forward * scanRange, 1f);
    }
    
    Vector3 GetConeDirection(Vector3 forward, Vector3 right, Vector3 up, float halfAngle, float circleAngle)
    {
        float halfAngleRad = halfAngle * Mathf.Deg2Rad;
        float circleAngleRad = circleAngle * Mathf.Deg2Rad;
        
        Vector3 coneDirection = forward * Mathf.Cos(halfAngleRad);
        coneDirection += (right * Mathf.Cos(circleAngleRad) + up * Mathf.Sin(circleAngleRad)) * Mathf.Sin(halfAngleRad);
        
        return coneDirection.normalized;
    }
}
