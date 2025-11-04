using UnityEngine;
using System.Collections;

public class ScanHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color highlightColor = Color.cyan;
    public float highlightDuration = 5f;
    public float pulseSpeed = 2f;
    
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;
    private bool isHighlighted = false;
    private Coroutine highlightCoroutine;
    
    void Start()
    {
        // Get all renderers on this object and children
        renderers = GetComponentsInChildren<Renderer>();
        SetupMaterials();
    }
    
    void SetupMaterials()
    {
        originalMaterials = new Material[renderers.Length];
        highlightMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            // Store original material
            originalMaterials[i] = renderers[i].material;
            
            // Create highlight material (simple emissive)
            highlightMaterials[i] = new Material(renderers[i].material);
            highlightMaterials[i].EnableKeyword("_EMISSION");
            highlightMaterials[i].SetColor("_EmissionColor", highlightColor);
            
            // If material doesn't support emission, try to make it glow another way
            if (!highlightMaterials[i].HasProperty("_EmissionColor"))
            {
                highlightMaterials[i].color = highlightColor;
            }
        }
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
        
        while (elapsedTime < highlightDuration)
        {
            // Pulse effect
            float pulse = Mathf.Sin(elapsedTime * pulseSpeed) * 0.5f + 0.5f;
            Color currentColor = highlightColor * pulse;
            
            // Apply highlight materials with pulsing
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
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
        
        isHighlighted = false;
    }
    
    public void StopHighlight()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }
        
        // Restore original materials immediately
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
        
        isHighlighted = false;
    }
}