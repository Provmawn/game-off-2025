Shader "Custom/ScanHighlight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HighlightColor ("Highlight Color", Color) = (0, 1, 1, 1)
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _Intensity ("Intensity", Float) = 2.0
        _RimPower ("Rim Power", Float) = 3.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Overlay+1"
            "RenderPipeline"="UniversalPipeline"
        }
        
        // First pass - render through walls
        Pass
        {
            Name "ThroughWalls"
            Tags { "LightMode"="UniversalForward" }
            
            ZWrite Off
            ZTest Always  // This makes it render through walls!
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _HighlightColor;
                float _PulseSpeed;
                float _Intensity;
                float _RimPower;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Calculate rim lighting
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float rimDot = 1.0 - dot(normalWS, viewDirWS);
                float rim = pow(rimDot, _RimPower);
                
                // Pulsing effect
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                
                // Combine effects
                half4 highlightColor = _HighlightColor;
                highlightColor.rgb *= _Intensity * (0.5 + pulse * 0.5);
                highlightColor.a = rim * highlightColor.a * (0.3 + pulse * 0.4);
                
                return highlightColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}