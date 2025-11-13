Shader "Custom/ScannerSphere"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Scanner Color", Color) = (1, 0.5, 0, 1)
        _ScanGlow ("Scan Glow", Float) = 5.0
        _Opacity ("Scanner Opacity", Range(0, 1)) = 0.5
        _RimPower ("Rim Power", Float) = 2.0
        _PulseSpeed ("Pulse Speed", Float) = 2.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _ScanGlow;
            float _Opacity;
            float _RimPower;
            float _PulseSpeed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Base color
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Rim lighting for sphere edge glow
                float rim = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
                rim = pow(rim, _RimPower);
                
                // Pulsing effect
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                
                // Combine effects
                col.rgb *= _ScanGlow * (rim + pulse * 0.3);
                col.a *= _Opacity * rim;
                
                return col;
            }
            ENDCG
        }
    }
}