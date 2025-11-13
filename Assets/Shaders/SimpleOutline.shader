Shader "Custom/SimpleOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.5, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        // First pass - render outline
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float4 _OutlineColor;
            float _OutlineWidth;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Expand vertex outward along normal
                float3 expandedPos = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(expandedPos);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}