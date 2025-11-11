using UnityEngine;
using System.Collections;

public class ScanGroundProjector : MonoBehaviour
{
    [Header("Ground Projection")]
    public Material projectionMaterial;
    public float coneAngle = 45f;
    public Color scanColor = Color.cyan;
    public float scanWidth = 2f;
    public float fadeDistance = 3f;
    
    [Header("Projection Quad")]
    public float quadSize = 150f;
    
    private GameObject projectionQuad;
    private MeshRenderer projectionRenderer;
    private bool isProjecting = false;
    
    private static readonly int ScanColorProperty = Shader.PropertyToID("_ScanColor");
    private static readonly int ScanWidthProperty = Shader.PropertyToID("_ScanWidth");
    private static readonly int FadeDistanceProperty = Shader.PropertyToID("_FadeDistance");
    private static readonly int ScanProgressProperty = Shader.PropertyToID("_ScanProgress");
    private static readonly int MaxRangeProperty = Shader.PropertyToID("_MaxRange");
    private static readonly int ConeAngleProperty = Shader.PropertyToID("_ConeAngle");
    private static readonly int ScannerPositionProperty = Shader.PropertyToID("_ScannerPosition");
    private static readonly int ScannerForwardProperty = Shader.PropertyToID("_ScannerForward");
    
    void Awake()
    {
        CreateProjectionQuad();
        CreateProjectionMaterial();
    }
    
    void CreateProjectionQuad()
    {
        projectionQuad = new GameObject("ScanProjectionQuad");
        projectionQuad.transform.parent = transform;
        
        MeshFilter meshFilter = projectionQuad.AddComponent<MeshFilter>();
        projectionRenderer = projectionQuad.AddComponent<MeshRenderer>();
        
        Mesh quadMesh = new Mesh();
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-quadSize/2, 0, -quadSize/2),
            new Vector3(quadSize/2, 0, -quadSize/2),
            new Vector3(quadSize/2, 0, quadSize/2),
            new Vector3(-quadSize/2, 0, quadSize/2)
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        quadMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        quadMesh.RecalculateNormals();
        
        meshFilter.mesh = quadMesh;
        
        projectionQuad.transform.localPosition = Vector3.zero;
        projectionQuad.transform.localRotation = Quaternion.Euler(0, 0, 0);
        
        projectionQuad.SetActive(false);
    }
    
    void CreateProjectionMaterial()
    {
        if (projectionMaterial == null)
        {
            Shader projectionShader = Shader.Find("Custom/ScanGroundProjection");
            if (projectionShader != null)
            {
                projectionMaterial = new Material(projectionShader);
                projectionMaterial.name = "ScanGroundProjectionMaterial";
            }
        }
        
        if (projectionMaterial != null)
        {
            projectionRenderer.material = projectionMaterial;
            UpdateMaterialProperties();
        }
    }
    
    void UpdateMaterialProperties()
    {
        if (projectionMaterial != null)
        {
            projectionMaterial.SetColor(ScanColorProperty, scanColor);
            projectionMaterial.SetFloat(ScanWidthProperty, scanWidth);
            projectionMaterial.SetFloat(FadeDistanceProperty, fadeDistance);
            projectionMaterial.SetFloat(ConeAngleProperty, coneAngle);
        }
    }
    
    public void StartProjection(Vector3 scannerPosition, Vector3 scannerForward, float maxRange, float duration)
    {
        if (isProjecting) return;
        
        Vector3 groundPosition = new Vector3(scannerPosition.x, scannerPosition.y, scannerPosition.z);
        transform.position = groundPosition;
        
        if (projectionMaterial != null)
        {
            projectionMaterial.SetVector(ScannerPositionProperty, scannerPosition);
            projectionMaterial.SetVector(ScannerForwardProperty, scannerForward);
            projectionMaterial.SetFloat(MaxRangeProperty, maxRange);
            UpdateMaterialProperties();
        }
        
        projectionQuad.SetActive(true);
        isProjecting = true;
        
        StartCoroutine(ProjectionAnimation(duration));
    }
    
    IEnumerator ProjectionAnimation(float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && projectionMaterial != null)
        {
            float progress = elapsedTime / duration;
            projectionMaterial.SetFloat(ScanProgressProperty, progress);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        projectionQuad.SetActive(false);
        isProjecting = false;
    }
    
    public void StopProjection()
    {
        StopAllCoroutines();
        projectionQuad.SetActive(false);
        isProjecting = false;
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && projectionMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }
}