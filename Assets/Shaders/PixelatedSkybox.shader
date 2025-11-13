Shader "Custom/PixelatedSkybox"
{
    Properties
    {
        _MainTex ("Skybox Texture", Cube) = "grey" {}
        _PixelSize ("Pixel Size", Float) = 64
        _Exposure ("Exposure", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            samplerCUBE _MainTex;
            float _PixelSize;
            float _Exposure;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Pixelate the texture coordinates
                float3 pixelatedCoord = round(i.texcoord * _PixelSize) / _PixelSize;
                
                // Sample the skybox
                fixed4 col = texCUBE(_MainTex, pixelatedCoord);
                col.rgb *= _Exposure;
                
                return col;
            }
            ENDCG
        }
    }
}