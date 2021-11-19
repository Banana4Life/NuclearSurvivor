// based on default shader from Polybrush
//// Shader created with Shader Forge v1.38 
//// Shader Forge (c) Freya Holmer - http://www.acegikmo.com/shaderforge/


Shader "BLF/TextureMix" {
    Properties {
        _Metallic ("Metallic", Range(0, 1)) = 0.25
        _Gloss ("Gloss", Range(0, 1)) = 0.5
        _ParallaxStrength ("HeightMap Strength", Range (0.005, 0.08)) = 0.02

        _Texture0 ("Texture 0 Albedo", 2D) = "albedo" {}
        _Texture0Normal ("Texture 0 Normal", 2D) = "normal" {}
        _Texture0Parallax ("Texture 0 Height", 2D) = "height" {}
        
        _Texture1 ("Texture 1 Albedo", 2D) = "albedo" {}
        _Texture1Normal ("Texture 1 Normal", 2D) = "normal" {}
        _Texture1Parallax ("Texture 1 Height", 2D) = "height" {}
        
        _Texture2 ("Texture 2 Albedo", 2D) = "albedo" {}
        _Texture2Normal ("Texture 2 Normal", 2D) = "normal" {}
        _Texture2Parallax ("Texture 2 Height", 2D) = "height" {}
        
        _Texture3 ("Texture 3 Albedo", 2D) = "albedo" {}
        _Texture3Normal ("Texture 3 Normal", 2D) = "normal" {}
        _Texture3Parallax ("Texture 3 Height", 2D) = "height" {}
        
        [Toggle] _ShowVertexColor ("Show Vertex Color", Float) = 0
        [Toggle] _UseHeightMap ("Use HeightMap", Float) = 1
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define SHOULD_SAMPLE_SH ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma target 3.0
            uniform float _Metallic;
            uniform float _Gloss;
            uniform sampler2D _Texture0; uniform float4 _Texture0_ST;
            uniform sampler2D _Texture1; uniform float4 _Texture1_ST;
            uniform sampler2D _Texture2; uniform float4 _Texture2_ST;
            uniform sampler2D _Texture3; uniform float4 _Texture3_ST;
            uniform sampler2D _Texture0Normal; uniform float4 _Texture0Normal_ST;
            uniform sampler2D _Texture1Normal; uniform float4 _Texture1Normal_ST;
            uniform sampler2D _Texture2Normal; uniform float4 _Texture2Normal_ST;
            uniform sampler2D _Texture3Normal; uniform float4 _Texture3Normal_ST;
            uniform sampler2D _Texture0Parallax; uniform float4 _Texture0Parallax_ST;
            uniform sampler2D _Texture1Parallax; uniform float4 _Texture1Parallax_ST;
            uniform sampler2D _Texture2Parallax; uniform float4 _Texture2Parallax_ST;
            uniform sampler2D _Texture3Parallax; uniform float4 _Texture3Parallax_ST;
            uniform float _ShowVertexColor;
            uniform float _UseHeightMap;
            uniform float _ParallaxStrength;
            
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                float4 vertexColor : COLOR;
                float3 viewDir : TEXCOORD3;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 posWorld : TEXCOORD4;
                float3 normalDir : TEXCOORD5;
                float3 tangentDir : TEXCOORD6;
                float3 bitangentDir : TEXCOORD7;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(7,8)
                UNITY_FOG_COORDS(9)
                float3 viewDir: TEXCOORD10;
                #if defined(LIGHTMAP_ON) || defined(UNITY_SHOULD_SAMPLE_SH)
                    float4 ambientOrLightmapUV : TEXCOORD11;
                #endif
                
            };
            
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                o.vertexColor = v.vertexColor;
                o.viewDir = v.viewDir;
                #ifdef LIGHTMAP_ON
                    o.ambientOrLightmapUV.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                    o.ambientOrLightmapUV.zw = 0;
                #elif UNITY_SHOULD_SAMPLE_SH
                #endif
                #ifdef DYNAMICLIGHTMAP_ON
                    o.ambientOrLightmapUV.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {

                if (_ShowVertexColor != 0)
                {
                    return i.vertexColor;
                }

                
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);

                float2 texOffset0 = ParallaxOffset(tex2D(_Texture0Parallax, i.uv0).r, _ParallaxStrength, i.viewDir);
                float2 texOffset1 = ParallaxOffset(tex2D(_Texture1Parallax, i.uv0).r, _ParallaxStrength, i.viewDir);
                float2 texOffset2 = ParallaxOffset(tex2D(_Texture2Parallax, i.uv0).r, _ParallaxStrength, i.viewDir);
                float2 texOffset3 = ParallaxOffset(tex2D(_Texture3Parallax, i.uv0).r, _ParallaxStrength, i.viewDir);

                if (_UseHeightMap == 0)
                {
                    texOffset0 = 0;
                    texOffset1 = 0;
                    texOffset2 = 0;
                    texOffset3 = 0;    
                }
                
                float4 node_6660 = normalize(float4(i.vertexColor.r,i.vertexColor.g,i.vertexColor.b,i.vertexColor.a));
                float3 _Texture0Normal_var = UnpackNormal(tex2D(_Texture0Normal,TRANSFORM_TEX(i.uv0 + texOffset0, _Texture0Normal)));
                float3 _Texture1Normal_var = UnpackNormal(tex2D(_Texture1Normal,TRANSFORM_TEX(i.uv0 + texOffset1, _Texture1Normal)));
                float3 _Texture2Normal_var = UnpackNormal(tex2D(_Texture2Normal,TRANSFORM_TEX(i.uv0 + texOffset2, _Texture2Normal)));
                float3 _Texture3Normal_var = UnpackNormal(tex2D(_Texture3Normal,TRANSFORM_TEX(i.uv0 + texOffset3, _Texture3Normal)));
                float3 normalLocal = (lerp( lerp( lerp( lerp( _Texture0Normal_var.rgb, _Texture0Normal_var.rgb, node_6660.r ), _Texture1Normal_var.rgb, node_6660.g ), _Texture2Normal_var.rgb, node_6660.b ), _Texture3Normal_var.rgb, node_6660.a ));
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
                float Pi = 3.141592654;
                float InvPi = 0.31830988618;
///////// Gloss:
                float gloss = _Gloss;
                float perceptualRoughness = 1.0 - _Gloss;
                float roughness = perceptualRoughness * perceptualRoughness;
                float specPow = exp2( gloss * 10.0 + 1.0 );
/////// GI Data:
                UnityLight light;
                #ifdef LIGHTMAP_OFF
                    light.color = lightColor;
                    light.dir = lightDirection;
                    light.ndotl = LambertTerm (normalDirection, light.dir);
                #else
                    light.color = half3(0.f, 0.f, 0.f);
                    light.ndotl = 0.0f;
                    light.dir = half3(0.f, 0.f, 0.f);
                #endif
                UnityGIInput d;
                d.light = light;
                d.worldPos = i.posWorld.xyz;
                d.worldViewDir = viewDirection;
                d.atten = attenuation;
                #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
                    d.ambient = 0;
                    d.lightmapUV = i.ambientOrLightmapUV;
                #else
                    d.ambient = i.ambientOrLightmapUV;
                #endif
                Unity_GlossyEnvironmentData ugls_en_data;
                ugls_en_data.roughness = 1.0 - gloss;
                ugls_en_data.reflUVW = viewReflectDirection;
                UnityGI gi = UnityGlobalIllumination(d, 1, normalDirection, ugls_en_data );
                lightDirection = gi.light.dir;
                lightColor = gi.light.color;
////// Specular:
                float NdotL = saturate(dot( normalDirection, lightDirection ));
                float LdotH = saturate(dot(lightDirection, halfDirection));
                float3 specularColor = _Metallic;
                float specularMonochrome;
                float4 _Texture0_var = tex2D(_Texture0,TRANSFORM_TEX(i.uv0 + texOffset0, _Texture0));
                float4 _Texture1_var = tex2D(_Texture1,TRANSFORM_TEX(i.uv0 + texOffset1, _Texture1));
                float4 _Texture2_var = tex2D(_Texture2,TRANSFORM_TEX(i.uv0 + texOffset2, _Texture2));
                float4 _Texture3_var = tex2D(_Texture3,TRANSFORM_TEX(i.uv0 + texOffset3, _Texture3));
                float3 diffuseColor = (lerp( lerp( lerp( lerp( _Texture0_var.rgb, _Texture0_var.rgb, node_6660.r ), _Texture1_var.rgb, node_6660.g ), _Texture2_var.rgb, node_6660.b ), _Texture3_var.rgb, node_6660.a )); // Need this for specular when using metallic
                diffuseColor = DiffuseAndSpecularFromMetallic( diffuseColor, specularColor, specularColor, specularMonochrome );
                specularMonochrome = 1.0-specularMonochrome;
                float NdotV = abs(dot( normalDirection, viewDirection ));
                float NdotH = saturate(dot( normalDirection, halfDirection ));
                float VdotH = saturate(dot( viewDirection, halfDirection ));
                float visTerm = SmithJointGGXVisibilityTerm( NdotL, NdotV, roughness );
                float normTerm = GGXTerm(NdotH, roughness);
                float specularPBL = (visTerm*normTerm) * UNITY_PI;
                #ifdef UNITY_COLORSPACE_GAMMA
                    specularPBL = sqrt(max(1e-4h, specularPBL));
                #endif
                specularPBL = max(0, specularPBL * NdotL);
                #if defined(_SPECULARHIGHLIGHTS_OFF)
                    specularPBL = 0.0;
                #endif
                specularPBL *= any(specularColor) ? 1.0 : 0.0;
                float3 directSpecular = attenColor*specularPBL*FresnelTerm(specularColor, LdotH);
                float3 specular = directSpecular;
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                half fd90 = 0.5 + 2 * LdotH * LdotH * (1-gloss);
                float nlPow5 = Pow5(1-NdotL);
                float nvPow5 = Pow5(1-NdotV);
                float3 directDiffuse = ((1 +(fd90 - 1)*nlPow5) * (1 + (fd90 - 1)*nvPow5) * NdotL) * attenColor;
                float3 indirectDiffuse = float3(0,0,0);
                indirectDiffuse += gi.indirect.diffuse;
                float3 diffuse = (directDiffuse + indirectDiffuse) * diffuseColor;
/// Final Color:
                float3 finalColor = diffuse + specular;
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
    }
    FallBack "Standard"
}
