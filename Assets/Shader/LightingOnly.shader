Shader "BLF/LightingOnly"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Blend SrcAlpha One
        ZWrite Off
        Tags { "Queue"="Transparent" }
        ColorMask RGB
        
        Pass
        {
            Tags {"LightMode" = "Vertex"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // for UnityObjectToWorldNormal

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal: NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed3 sh : COLOR0;
                float3 vLights : COLOR1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float3 ShadeVertexLightsFullCustom (float4 vertex, float3 normal, int lightCount, bool spotLight)
            {
                float3 viewpos = UnityObjectToViewPos (vertex.xyz);
                float3 viewN = normalize (mul ((float3x3)UNITY_MATRIX_IT_MV, normal));

                float3 lightColor = 0;
                for (int i = 0; i < lightCount; i++) {
                    float3 toLight = unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;
                    float lengthSq = dot(toLight, toLight);

                    // don't produce NaNs if some vertex position overlaps with the light
                    lengthSq = max(lengthSq, 0.000001);

                    toLight *= rsqrt(lengthSq);

                    float atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);
                    if (spotLight)
                    {
                        float rho = max (0, dot(toLight, unity_SpotDirection[i].xyz));
                        float spotAtt = (rho - unity_LightAtten[i].x) * unity_LightAtten[i].y;
                        atten *= saturate(spotAtt);
                    }

                    float diff = max (0, dot (viewN, toLight));
                    lightColor += unity_LightColor[i].rgb * (diff * atten);
                }
                return lightColor;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vLights = ShadeVertexLightsFullCustom(v.vertex, v.normal, 4, true);
                // o.sh = ShadeSH9(fixed4(UnityObjectToWorldNormal(v.normal), 1));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed3 vlights = i.vLights.rgb * 2;
                col.rgb *= vlights;
                return fixed4(col.rgb, 1);
            }
            ENDCG
        }
    }
}