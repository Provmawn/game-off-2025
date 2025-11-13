Shader "Custom/ScanGroundProjection"
{
    Properties
    {
        _ScanColor ("Scan Color", Color) = (0, 1, 1, 0.5)
        _ScanWidth ("Scan Width", Float) = 0.1
        _FadeDistance ("Fade Distance", Float) = 2.0
        _ScanProgress ("Scan Progress", Range(0, 1)) = 0.0
        _MaxRange ("Max Range", Float) = 60.0
        _ConeAngle ("Cone Angle", Float) = 45.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Overlay"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "GroundProjection"
            Tags { "LightMode"="UniversalForward" }
            
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ScanColor;
                float _ScanWidth;
                float _FadeDistance;
                float _ScanProgress;
                float _MaxRange;
                float _ConeAngle;
                float4x4 _ScannerMatrix;
                float3 _ScannerPosition;
                float3 _ScannerForward;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.worldPos = vertexInput.positionWS;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Calculate distance and direction from scanner
                float3 toPixel = input.worldPos - _ScannerPosition;
                float distance = length(toPixel);
                float3 direction = toPixel / distance;
                
                // Project to ground plane (assume Y is up)
                float3 groundDirection = normalize(float3(direction.x, 0, direction.z));
                float3 scanForward = normalize(float3(_ScannerForward.x, 0, _ScannerForward.z));
                
                // Calculate angle from scan direction
                float dotProduct = dot(groundDirection, scanForward);
                float angle = acos(saturate(dotProduct)) * 180.0 / 3.14159;
                
                // Check if within cone angle
                float coneHalfAngle = _ConeAngle * 0.5;
                if (angle > coneHalfAngle)
                    discard;
                
                // Calculate scan progress distance
                float currentScanDistance = _ScanProgress * _MaxRange;
                
                // Create the moving scan line
                float distanceFromScanLine = abs(distance - currentScanDistance);
                float scanLineAlpha = 1.0 - saturate(distanceFromScanLine / _ScanWidth);
                
                // Create fade for edges
                float edgeFade = 1.0 - saturate((distance - currentScanDistance + _FadeDistance) / _FadeDistance);
                edgeFade = saturate(edgeFade);
                
                // Cone edge fade
                float angleNormalized = angle / coneHalfAngle;
                float coneFade = 1.0 - pow(angleNormalized, 2.0);
                
                // Combine all factors
                float alpha = scanLineAlpha * edgeFade * coneFade * _ScanColor.a;
                
                if (alpha < 0.01)
                    discard;
                
                return float4(_ScanColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}