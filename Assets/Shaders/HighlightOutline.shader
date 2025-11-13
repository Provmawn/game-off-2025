Shader "Custom/HighlightOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.5, 0, 1)
        _OutlineThickness ("Outline Thickness", Float) = 5.0
        _OutlineGlow ("Outline Glow", Float) = 10.0
        _PulseSpeed ("Pulse Speed", Float) = 2.0
    }
    
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100
        
        // Outline pass
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineGlow;
            float _PulseSpeed;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Expand vertex along normal for outline effect
                float3 normal = normalize(v.normal);
                float3 outlinePos = v.vertex.xyz + normal * (_OutlineThickness * 0.01);
                
                o.vertex = UnityObjectToClipPos(float4(outlinePos, 1.0));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Pulsing glow effect
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float4 col = _OutlineColor;
                col.rgb *= _OutlineGlow * (0.7 + pulse * 0.3);
                col.a = 0.8 + pulse * 0.2;
                
                return col;
            }
            ENDCG
        }
    }
}