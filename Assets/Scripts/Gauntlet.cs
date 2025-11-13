 using UnityEngine;
using System.Collections.Generic;
public class Gauntlet : MonoBehaviour
{
    [Header("Heat System")]
    float maxHeat = 100f;
    float currentHeat = 0f;
    bool isOverheated = false;
    float overheatDuration = 10f;
    float overheatFinishes = 0f;
    float heatdecayRate = 1f;
    [Header("Scanning")]
    public float scanCooldown = 1f;
    float lastScanTime = -10f;
    public float scanRange = 20f;
    public float scanConeAngle = 90f;
    [Header("Scan Layers")]
    public LayerMask scanLayers = (1 << 7) | (1 << 8);
    [Header("Scan Animation")]
    public float scanDuration = 2f;
    public float maxScanRadius = 30f;
    public float baseSphereRadius = 0.5f;
    public GameObject scanEffectPrefab;
    [Header("Animation")]
    public Animator gauntletAnimator;
    public string fistTriggerName = "Fist";
    [Header("Audio")]
    public AudioSource gauntletAudioSource;
    public float audioVolume = 1f;
    [Header("Highlight System")]
    private Material highlightMaterial;
    public float highlightDuration = 5f;
    public Color highlightColor = Color.cyan;
    [Header("Ground Projection")]
    private ScanGroundProjector groundProjector;
    private HashSet<GameObject> currentlyHighlighted = new HashSet<GameObject>();
    [Header("Debug Visualization")]
    public bool showScanCone = true;
    public Color scanConeColor = Color.green;
    public int coneResolution = 20;

    public ItemHolder ItemHolder;

    void Start()
    {
        if (gauntletAnimator == null)
        {
        }
        if (gauntletAudioSource == null)
        {
        }
        else if (gauntletAudioSource.clip == null)
        {
        }
        InitializeHighlightSystem();
        InitializeGroundProjector();
    }
    void InitializeHighlightSystem()
    {
        if (highlightMaterial == null)
        {
            CreateHighlightMaterial();
        }
    }
    void CreateHighlightMaterial()
    {
        Shader highlightShader = Shader.Find("Custom/ScanHighlight");
        if (highlightShader != null)
        {
            highlightMaterial = new Material(highlightShader);
            highlightMaterial.name = "ScanHighlightMaterial";
            highlightMaterial.SetColor("_HighlightColor", highlightColor);
            highlightMaterial.SetFloat("_PulseSpeed", 3f);
            highlightMaterial.SetFloat("_Intensity", 2f);
            highlightMaterial.SetFloat("_RimPower", 2f);
        }
        else
        {
        }
    }
    void InitializeGroundProjector()
    {
        GameObject projectorObj = new GameObject("GroundProjector");
        projectorObj.transform.position = Vector3.zero;
        groundProjector = projectorObj.AddComponent<ScanGroundProjector>();
        if (groundProjector != null)
        {
            groundProjector.scanColor = highlightColor;
            groundProjector.coneAngle = scanConeAngle;
        }
        else
        {
        }
    }
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
        TriggerFistAnimation();
        OnAnimationScanTrigger();
        if (currentHeat >= maxHeat)
        {
            Overheat();
        }
    }
    System.Collections.IEnumerator PerformScanWithAnimationSync()
    {
        float animationDelay = 0.5f;
        yield return new WaitForSeconds(animationDelay);
        yield return StartCoroutine(PerformScan());
    }
    public void OnAnimationScanTrigger()
    {
        PlayScanSound();
        if (groundProjector != null)
        {
            float actualScanRange = Mathf.Min(scanRange, maxScanRadius);
            Vector3 gauntletPos = transform.position;
            Vector3 groundPosition = gauntletPos;
            RaycastHit hit;
            if (Physics.Raycast(gauntletPos, Vector3.down, out hit, 100f))
            {
                groundPosition = hit.point;
            }
            else
            {
                groundPosition = new Vector3(gauntletPos.x, 0f, gauntletPos.z);
            }
            Vector3 forwardDirection = transform.forward;
            groundProjector.StartProjection(groundPosition, forwardDirection, actualScanRange, scanDuration);
        }
        StartCoroutine(PerformScan());
    }
    System.Collections.IEnumerator PerformScan()
    {
        float elapsed = 0f;
        float currentDistance = 0f;
        HashSet<GameObject> scannedObjects = new HashSet<GameObject>();
        float actualScanRange = Mathf.Min(scanRange, maxScanRadius);
        while (elapsed < scanDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / scanDuration;
            currentDistance = Mathf.Lerp(0f, actualScanRange, progress);
            float coneRadiusAtDistance = currentDistance * Mathf.Tan((scanConeAngle / 2f) * Mathf.Deg2Rad);
            float sphereRadius = Mathf.Max(baseSphereRadius, coneRadiusAtDistance);
            Vector3 spherePosition = transform.position + (transform.forward * currentDistance);
            Collider[] hits = Physics.OverlapSphere(spherePosition, sphereRadius, scanLayers);
            foreach (Collider hit in hits)
            {
                if (scannedObjects.Contains(hit.gameObject))
                    continue;
                if (hit.CompareTag("Player") || hit.name.Contains("Player"))
                    continue;
                Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                if (angleToTarget <= scanConeAngle / 2f)
                {
                    float distanceToObject = Vector3.Distance(transform.position, hit.transform.position);
                    if (distanceToObject <= actualScanRange)
                    {
                        ScanObject(hit.gameObject);
                        scannedObjects.Add(hit.gameObject);
                    }
                }
            }
            yield return null;
        }
    }
    void ScanObject(GameObject obj)
    {
        if (currentlyHighlighted.Contains(obj))
        {
            return;
        }
        if (highlightMaterial != null)
        {
            currentlyHighlighted.Add(obj);
            StartCoroutine(ApplyBuiltInHighlight(obj));
        }
        IScannable scannable = obj.GetComponent<IScannable>();
        if (scannable != null)
        {
            scannable.OnScanned();
        }
    }
    System.Collections.IEnumerator ApplyBuiltInHighlight(GameObject obj)
    {
        if (obj == null)
        {
            yield break;
        }
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0)
        {
            yield break;
        }
        Material[][] originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null) continue;
            originalMaterials[i] = renderer.materials;
            Material[] newMaterials = new Material[originalMaterials[i].Length + 1];
            for (int j = 0; j < originalMaterials[i].Length; j++)
            {
                newMaterials[j] = originalMaterials[i][j];
            }
            newMaterials[newMaterials.Length - 1] = highlightMaterial;
            renderer.materials = newMaterials;
        }
        float elapsed = 0f;
        while (elapsed < highlightDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].materials = originalMaterials[i];
            }
        }
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
                renderer.enabled = true;
            }
        }
        currentlyHighlighted.Remove(obj);
    }
    public void TriggerFistAnimation()
    {
        if (gauntletAnimator != null)
        {
            AnimatorStateInfo currentState = gauntletAnimator.GetCurrentAnimatorStateInfo(0);
            gauntletAnimator.SetTrigger(fistTriggerName);
            StartCoroutine(CheckAnimationAfterFrame());
        }
        else
        {
        }
    }
    System.Collections.IEnumerator CheckAnimationAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        if (gauntletAnimator != null)
        {
            AnimatorStateInfo newState = gauntletAnimator.GetCurrentAnimatorStateInfo(0);
            if (gauntletAnimator.IsInTransition(0))
            {
                AnimatorTransitionInfo transitionInfo = gauntletAnimator.GetAnimatorTransitionInfo(0);
            }
        }
    }
    string GetStateName(int hash)
    {
        if (hash == Animator.StringToHash("Idle")) return "Idle";
        if (hash == Animator.StringToHash("MakeFist")) return "MakeFist";
        if (hash == Animator.StringToHash("Fist")) return "Fist";
        return $"Hash:{hash}";
    }
    bool HasParameter(Animator animator, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }
        return false;
    }
    void PlayScanSound()
    {
        if (gauntletAudioSource != null && gauntletAudioSource.clip != null)
        {
            gauntletAudioSource.volume = audioVolume;
            gauntletAudioSource.Play();
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
