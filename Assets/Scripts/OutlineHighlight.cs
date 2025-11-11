using UnityEngine;

public class OutlineHighlight : MonoBehaviour, IScannable
{
    [Header("Outline Settings")]
    public Material outlineMaterial;
    public float highlightDuration = 3f;
    
    [Header("Scan Info")]
    public string scanDisplayName = "Unknown Object";
    public ScanType objectType = ScanType.Item;
    
    private GameObject outlineObject;
    private bool isHighlighted = false;
    private float highlightTimer = 0f;
    
    void Start()
    {
        SetupOutlineObject();
    }
    
    void SetupOutlineObject()
    {
        if (outlineMaterial == null) return;
        
        outlineObject = new GameObject(gameObject.name + "_Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        
        MeshFilter parentMeshFilter = GetComponent<MeshFilter>();
        if (parentMeshFilter != null)
        {
            MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
            outlineMeshFilter.mesh = parentMeshFilter.mesh;
            
            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.material = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
        }
        
        outlineObject.SetActive(false);
    }
    
    void Update()
    {
        if (isHighlighted)
        {
            highlightTimer -= Time.deltaTime;
            if (highlightTimer <= 0f)
            {
                StopHighlight();
            }
        }
    }
    
    public void OnScanned()
    {
        StartHighlight();
        
    }
    
    public void StartHighlight()
    {
        if (outlineObject != null)
        {
            outlineObject.SetActive(true);
            isHighlighted = true;
            highlightTimer = highlightDuration;
        }
    }
    
    public void StopHighlight()
    {
        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }
        isHighlighted = false;
    }
    
    public string GetScanInfo()
    {
        return scanDisplayName;
    }
    
    public ScanType GetScanType()
    {
        return objectType;
    }
    
    void OnDestroy()
    {
        if (outlineObject != null)
        {
            DestroyImmediate(outlineObject);
        }
    }
}