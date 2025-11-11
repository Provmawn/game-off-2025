using UnityEngine;
using System.Collections;

public class ScanHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = Color.cyan;
    public float highlightDuration = 5f;
    public float pulseSpeed = 2f;
    public bool renderThroughWalls = true;
    
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;
    private GameObject[] outlineObjects;
    private bool isHighlighted = false;
    private Coroutine highlightCoroutine;
    
    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetupMaterials();
    }
    
    void SetupMaterials()
    {
        originalMaterials = new Material[renderers.Length];
        highlightMaterials = new Material[renderers.Length];
        
        if (renderThroughWalls)
        {
            outlineObjects = new GameObject[renderers.Length];
        }
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
            
            highlightMaterials[i] = new Material(renderers[i].material);
            highlightMaterials[i].EnableKeyword("_EMISSION");
            highlightMaterials[i].SetColor("_EmissionColor", highlightColor);
            
            if (!highlightMaterials[i].HasProperty("_EmissionColor"))
            {
                highlightMaterials[i].color = highlightColor;
            }
            
            if (renderThroughWalls)
            {
                CreateOutlineObject(i);
            }
        }
    }
    
    void CreateOutlineObject(int index)
    {
        GameObject outlineObj = new GameObject($"{gameObject.name}_Outline_{index}");
        outlineObj.transform.SetParent(transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one * 1.02f;
        
        MeshFilter originalMesh = renderers[index].GetComponent<MeshFilter>();
        if (originalMesh != null)
        {
            MeshFilter outlineMesh = outlineObj.AddComponent<MeshFilter>();
            outlineMesh.mesh = originalMesh.mesh;
        }
        
        MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
        
        Material outlineMat = CreateThroughWallMaterial();
        outlineRenderer.material = outlineMat;
        outlineRenderer.enabled = false;
        
        outlineObjects[index] = outlineObj;
    }
    
    Material CreateThroughWallMaterial()
    {
        string[] shaderNames = {
            "Unlit/Color",
            "Legacy Shaders/Unlit/Color",
            "Sprites/Default",
            "UI/Default"
        };
        
        Material mat = null;
        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                mat = new Material(shader);
                break;
            }
        }
        
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
        }
        
        mat.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.5f);
        
        if (mat.HasProperty("_ZTest"))
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        
        mat.renderQueue = 3000;
        
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        
        return mat;
    }
    
    public void StartHighlight()
    {
        if (isHighlighted) return;
        
        isHighlighted = true;
        if (highlightCoroutine != null) StopCoroutine(highlightCoroutine);
        highlightCoroutine = StartCoroutine(HighlightCoroutine());
    }
    
    IEnumerator HighlightCoroutine()
    {
        float elapsedTime = 0f;
        
        if (renderThroughWalls && outlineObjects != null)
        {
            for (int i = 0; i < outlineObjects.Length; i++)
            {
                if (outlineObjects[i] != null)
                {
                    outlineObjects[i].GetComponent<MeshRenderer>().enabled = true;
                }
            }
        }
        
        while (elapsedTime < highlightDuration)
        {
            float pulse = Mathf.Sin(elapsedTime * pulseSpeed) * 0.5f + 0.5f;
            Color currentColor = highlightColor * pulse;
            
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = highlightMaterials[i];
                
                if (highlightMaterials[i].HasProperty("_EmissionColor"))
                {
                    highlightMaterials[i].SetColor("_EmissionColor", currentColor);
                }
                else
                {
                    highlightMaterials[i].color = Color.Lerp(originalMaterials[i].color, currentColor, pulse);
                }
            }
            
            if (renderThroughWalls && outlineObjects != null)
            {
                for (int i = 0; i < outlineObjects.Length; i++)
                {
                    if (outlineObjects[i] != null)
                    {
                        MeshRenderer outlineRenderer = outlineObjects[i].GetComponent<MeshRenderer>();
                        if (outlineRenderer != null)
                        {
                            Color outlineColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.3f + pulse * 0.3f);
                            outlineRenderer.material.color = outlineColor;
                        }
                    }
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
        
        if (renderThroughWalls && outlineObjects != null)
        {
            for (int i = 0; i < outlineObjects.Length; i++)
            {
                if (outlineObjects[i] != null)
                {
                    outlineObjects[i].GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
        
        isHighlighted = false;
    }
    
    public void StopHighlight()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }
        
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
        
        if (renderThroughWalls && outlineObjects != null)
        {
            for (int i = 0; i < outlineObjects.Length; i++)
            {
                if (outlineObjects[i] != null)
                {
                    outlineObjects[i].GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
        
        isHighlighted = false;
    }
}