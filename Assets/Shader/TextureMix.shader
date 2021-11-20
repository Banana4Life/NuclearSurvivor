// based on default shader from Polybrush
//// Shader created with Shader Forge v1.38 
//// Shader Forge (c) Freya Holmer - http://www.acegikmo.com/shaderforge/


Shader "BFL/TextureMix" {
    Properties {
        [Toggle] _ShowVertexColor ("Show Vertex Color", Float) = 0
        [Toggle] _UseHeightMap ("Use HeightMap", Float) = 1
        
        _Metallic ("Metallic", Range(0, 1)) = 0.25
        _Gloss ("Gloss", Range(0, 1)) = 0.5
        _NormalStrength ("NormalMap Strength",Float) = 1
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
            #include "BLFCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma target 3.0
            #pragma shader_feature_local _PARALLAXMAP
            
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
                float3 viewDirForParallax : TEXCOORD10;
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
                TANGENT_SPACE_ROTATION;
                o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
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

			    float2 texOffset0 = 0; float2 texOffset1 = 0; float2 texOffset2 = 0; float2 texOffset3 = 0;
			    if (_UseHeightMap)
			    {
			        ParallaxOffsets(i.uv0, _ParallaxStrength, i.viewDirForParallax,
                        _Texture0Parallax,_Texture1Parallax,_Texture2Parallax,_Texture3Parallax,
                        texOffset0, texOffset1, texOffset2, texOffset3
                    );    
			    }
                
                float3 normalLocal = MixedTexture(_Texture0Normal, _Texture1Normal, _Texture2Normal, _Texture3Normal,
                                                   _Texture0Normal_ST, _Texture1Normal_ST, _Texture2Normal_ST, _Texture3Normal_ST,
                                                   texOffset0, texOffset1, texOffset2, texOffset3,
                                                   i.vertexColor);
                 float albedo = MixedTexture(_Texture0, _Texture1, _Texture2, _Texture3,
                                           _Texture0_ST, _Texture1_ST, _Texture2_ST, _Texture3_ST,
                                           texOffset0, texOffset1, texOffset2, texOffset3,
                                           i.vertexColor);


                
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
                
     
                float3 specularColor;
                float diffuseColor = DiffuseAndSpecular(albedo, _Metallic, specularColor);
                float NdotV = abs(dot( normalDirection, viewDirection ));
                float NdotH = saturate(dot( normalDirection, halfDirection ));
                float VdotH = saturate(dot( viewDirection, halfDirection ));
                float NdotL = saturate(dot( normalDirection, lightDirection ));
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
                float LdotH = saturate(dot(lightDirection, halfDirection));
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
        Pass {
            Name "FORWARD_DELTA"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One


            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define SHOULD_SAMPLE_SH ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "BLFCG.cginc"
            #pragma multi_compile_fwdadd_fullshadows
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
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 posWorld : TEXCOORD3;
                float3 normalDir : TEXCOORD4;
                float3 tangentDir : TEXCOORD5;
                float3 bitangentDir : TEXCOORD6;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(7,8)
                UNITY_FOG_COORDS(9)
                float3 viewDirForParallax : TEXCOORD10;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                o.vertexColor = v.vertexColor;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                TANGENT_SPACE_ROTATION;
                o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                if (_ShowVertexColor != 0)
                {
                    return i.vertexColor;
                }
			    float2 texOffset0 = 0; float2 texOffset1 = 0; float2 texOffset2 = 0; float2 texOffset3 = 0;
			    if (_UseHeightMap)
			    {
			        ParallaxOffsets(i.uv0, _ParallaxStrength, i.viewDirForParallax,
                        _Texture0Parallax,_Texture1Parallax,_Texture2Parallax,_Texture3Parallax,
                        texOffset0, texOffset1, texOffset2, texOffset3
                    );    
			    }
                
                float3 normalLocal = MixedTexture(_Texture0Normal, _Texture1Normal, _Texture2Normal, _Texture3Normal,
                                                                   _Texture0Normal_ST, _Texture1Normal_ST, _Texture2Normal_ST, _Texture3Normal_ST,
                                                                   texOffset0, texOffset1, texOffset2, texOffset3,
                                                                   i.vertexColor);
                float3 albedo = MixedTexture(_Texture0, _Texture1, _Texture2, _Texture3,
                                                   _Texture0_ST, _Texture1_ST, _Texture2_ST, _Texture3_ST,
                                                   texOffset0, texOffset1, texOffset2, texOffset3,
                                                   i.vertexColor);
                float3 specularColor;
                float3 diffusedColor = DiffuseAndSpecular(albedo, _Metallic, specularColor);

                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
///////// Gloss:
                float perceptualRoughness = 1.0 - _Gloss;
                float roughness = perceptualRoughness * perceptualRoughness;
////// Specular:
                float NdotV = abs(dot( normalDirection, viewDirection ));
                float NdotH = saturate(dot( normalDirection, halfDirection ));
                float VdotH = saturate(dot( viewDirection, halfDirection ));
                float NdotL = saturate(dot( normalDirection, lightDirection ));
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
                float LdotH = saturate(dot(lightDirection, halfDirection));
                float3 directSpecular = attenColor*specularPBL*FresnelTerm(specularColor, LdotH);
                float3 specular = directSpecular;
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                half fd90 = 0.5 + 2 * LdotH * LdotH * (1-_Gloss);
                float nlPow5 = Pow5(1-NdotL);
                float nvPow5 = Pow5(1-NdotV);
                float3 directDiffuse = ((1 +(fd90 - 1)*nlPow5) * (1 + (fd90 - 1)*nvPow5) * NdotL) * attenColor;
                float3 diffuse = directDiffuse * diffusedColor;
/// Final Color:
                float3 finalColor = diffuse + specular;
                fixed4 finalRGBA = fixed4(finalColor * 1,0);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Tags { "LightMode"="Deferred" }
            
            CGPROGRAM
			#pragma vertex vertDeferred
			#pragma fragment fragDeferred
			#pragma exclude_renderers nomrt
			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma target 3.0
            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "BLFCG.cginc"
			
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
            uniform float _NormalStrength;
            
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };

            struct VertexOutput {
                UNITY_POSITION(pos);
                float2 tex0                  : TEXCOORD0;
                float3 eyeVec               : TEXCOORD1;
                float3 tangent              : TEXCOORD2;
                half3 binormal              : TEXCOORD3;
                half3 normal                : TEXCOORD4;
                half3 posWorld              : TEXCOORD5;
                half3 viewDirForParallax    : TEXCOORD6;
                half4 ambientOrLightmapUV   : TEXCOORD7 ;    // SH or Lightmap UVs
                float4 vertexColor          : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
			
			struct FragOut
			{
				half4 albedo : SV_Target0;
				half4 specular : SV_Target1;
				half4 normal : SV_Target2;
				half4 emission : SV_Target3;
			};
			
			VertexOutput vertDeferred (VertexInput v) 
			{
                VertexOutput o;

			    o.posWorld = mul(unity_ObjectToWorld, v.vertex);
			    o.pos = UnityObjectToClipPos( v.vertex );
                // TODO if textures have different scale this needs to be calculated for each texture
			    o.tex0 = TRANSFORM_TEX(v.uv0, _Texture0); // Transforms 2D UV by scale/bias property  
			    o.eyeVec = normalize(o.posWorld.xyz - _WorldSpaceCameraPos);
                float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			    float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
			    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
			    o.tangent = tangentToWorld[0];
			    o.binormal = tangentToWorld[1];
			    o.normal = tangentToWorld[2];
                TANGENT_SPACE_ROTATION;
			    o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
				o.vertexColor = v.vertexColor;
				o.ambientOrLightmapUV = 0;
				// ? LightMap
				o.ambientOrLightmapUV.rgb = ShadeSHPerVertex(normalWorld, o.ambientOrLightmapUV.rgb); 
                return o;
			}
			
			FragOut fragDeferred (VertexOutput i)
			{
				FragOut o;
			    if (_ShowVertexColor != 0)
                {
			        o.albedo = normalize(i.vertexColor);
			        o.specular = i.vertexColor;
			        o.normal = half4( i.normal * 0.5 + 0.5, 1.0 );
			        o.emission = half4(0,0,0,0);
			        #ifndef UNITY_HDR_ON
					    o.emission.rgb = exp2(-o.emission.rgb);
				    #endif
			        return o;
                }

                // Calculate Texture UVs
			    float2 tex0 = 0; float2 tex1 = 0; float2 tex2 = 0; float2 tex3 = 0;
			    if (_UseHeightMap)
			    {
			        ParallaxOffsets(i.tex0, _ParallaxStrength, i.viewDirForParallax,
                        _Texture0Parallax,_Texture1Parallax,_Texture2Parallax,_Texture3Parallax,
                        tex0, tex1, tex2, tex3
                    );    
			    }

				float4 albedo = MixedTexture(_Texture0, _Texture1, _Texture2, _Texture3,
                                               _Texture0_ST, _Texture1_ST, _Texture2_ST, _Texture3_ST,
                                               tex0, tex1, tex2, tex3,
                                               i.vertexColor);
				// UnityStandardInput.Albedo (we have no details) 
			    
			    half4 specGloss = _Gloss; // UnityStandardInput.SpecularGloss
			    half3 specColor = specGloss.rgb;
                half smoothness = specGloss.a;

                half oneMinusReflectivity;
                half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular (albedo, specColor, oneMinusReflectivity);
					        		    

				float4 normalLocal = MixedTexture(_Texture0Normal, _Texture1Normal, _Texture2Normal, _Texture3Normal,
                                                                   _Texture0Normal_ST, _Texture1Normal_ST, _Texture2Normal_ST, _Texture3Normal_ST,
                                                                   tex0, tex1, tex2, tex3,
                                                                   i.vertexColor);

				// NormalInTangentSpace
				half3 normalTangent = UnpackScaleNormal(normalLocal, _NormalStrength);
				// PerPixelWorldNormal
				float3 normalWorld = normalize(i.tangent * normalTangent.x + i.binormal * normalTangent.y + i.normal * normalTangent.z);

			    UNITY_SETUP_INSTANCE_ID(i);

				// ? Occlusion Map/Strength
				half atten = 1;
				half occlusion = 1;
				// FragmentGI
				bool sampleReflectionsInDeferred = false;
				UnityGI gi = BFL_FragmentGI(i.posWorld, normalize(i.eyeVec), smoothness, normalWorld, specColor,
					occlusion, i.ambientOrLightmapUV, atten, sampleReflectionsInDeferred);
				half3 emissiveColor = BRDF3_Unity_PBS(diffColor, specColor, oneMinusReflectivity, smoothness, normalWorld, -normalize(i.eyeVec), gi.light, gi.indirect).rgb;
				
				// ? Emission += tex lookup
				#ifndef UNITY_HDR_ON
					emissiveColor = exp2(-emissiveColor.rgb);
				#endif
				
				o.albedo = half4( diffColor, occlusion);
				o.specular = half4( specColor, smoothness );
				o.normal = half4( normalWorld * 0.5f + 0.5f, 1.0 );
				o.emission = half4(emissiveColor, 1);
				
				return o;
			}
			ENDCG
        }
        Pass {
            Name "Meta"
            Tags {
                "LightMode"="Meta"
            }
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_META 1
            #define SHOULD_SAMPLE_SH ( defined (LIGHTMAP_OFF) && defined(DYNAMICLIGHTMAP_OFF) )
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityMetaPass.cginc"
            #include "BLFCG.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
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
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 posWorld : TEXCOORD3;
                float4 vertexColor : COLOR;
                float3 viewDirForParallax: TEXCOORD4;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.uv2 = v.texcoord2;
                o.vertexColor = v.vertexColor;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST );                
                TANGENT_SPACE_ROTATION;
                o.viewDirForParallax = o.viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));

                return o;
            }
            float4 frag(VertexOutput i) : SV_Target {

                if (_ShowVertexColor != 0)
                {
                    return i.vertexColor;
                }

                
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT( UnityMetaInput, o );

                o.Emission = 0;

	            float2 texOffset0 = 0; float2 texOffset1 = 0; float2 texOffset2 = 0; float2 texOffset3 = 0;
			    if (_UseHeightMap)
			    {
			        ParallaxOffsets(i.uv0, _ParallaxStrength, i.viewDirForParallax,
                        _Texture0Parallax,_Texture1Parallax,_Texture2Parallax,_Texture3Parallax,
                        texOffset0, texOffset1, texOffset2, texOffset3
                    );    
			    }
                
                float3 albedo = MixedTexture(_Texture0, _Texture1, _Texture2, _Texture3,
                                                   _Texture0_ST, _Texture1_ST, _Texture2_ST, _Texture3_ST,
                                                   texOffset0, texOffset1, texOffset2, texOffset3,
                                                   i.vertexColor);
                float3 specular;
                float3 diffuse = DiffuseAndSpecular(albedo, _Metallic, specular);
                float roughness = 1.0 - _Gloss;
                o.Albedo = diffuse + specular * roughness * roughness * 0.5;

                return UnityMetaFragment( o );
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}
