
// Computes ParallaxOffset based on HeightMap Textures
void ParallaxOffsets(float2 uv, float parallaxStrength, float3 viewDirForParallax,
                     sampler2D _Texture0Parallax, sampler2D _Texture1Parallax, sampler2D _Texture2Parallax, sampler2D _Texture3Parallax,
                     out float2 tex0, out float2 tex1, out float2 tex2, out float2 tex3
    )
{
    tex0 = uv + ParallaxOffset(tex2D(_Texture0Parallax, uv).r, parallaxStrength, viewDirForParallax); 
    tex1 = uv + ParallaxOffset(tex2D(_Texture1Parallax, uv).r, parallaxStrength, viewDirForParallax); 
    tex2 = uv + ParallaxOffset(tex2D(_Texture2Parallax, uv).r, parallaxStrength, viewDirForParallax); 
    tex3 = uv + ParallaxOffset(tex2D(_Texture3Parallax, uv).r, parallaxStrength, viewDirForParallax); 
}



// Mixes 4 Textures
float4 MixedTexture(sampler2D _Texture0, sampler2D _Texture1, sampler2D _Texture2, sampler2D _Texture3,
                    float4 _Texture0_ST, float4 _Texture1_ST, float4 _Texture2_ST, float4 _Texture3_ST,
                    float2 tex0, float2 tex1, float2 tex2, float2 tex3,
                    float4 mixing)
{
    float4 color0 = tex2D(_Texture0,TRANSFORM_TEX(tex0, _Texture0));
    float4 color1 = tex2D(_Texture1,TRANSFORM_TEX(tex1, _Texture1));
    float4 color2 = tex2D(_Texture2,TRANSFORM_TEX(tex2, _Texture2));
    float4 color3 = tex2D(_Texture3,TRANSFORM_TEX(tex3, _Texture3));

    float4 lerp0 = 0;
    float4 lerp1 = lerp(lerp0, color0, mixing.r);
    float4 lerp2 = lerp(lerp1, color1, mixing.g);
    float4 lerp3 = lerp(lerp2, color2, mixing.b);
    float4 lerp4 = lerp(lerp3, color3, mixing.a);
    
    float4 albedo = lerp4;
    return albedo;
}

float3 DiffuseAndSpecular(float3 albedo, float3 metallic, out float3 specular)
{
    float specularMonochrome;
    float3 diffuseColor = DiffuseAndSpecularFromMetallic( albedo, metallic, specular, specularMonochrome );
    return diffuseColor;
}


UnityLight DummyLight()
{
    UnityLight l;
    l.color = 0;
    l.dir = half3 (0,1,0);
    return l;
}

inline UnityGI BFL_FragmentGI (half3 posWorld, float3 eyeVec, half smoothness, float3 normalWorld, half3 specColor,
    half occlusion, half4 i_ambientOrLightmapUV, half atten, bool reflections)
{
    UnityGIInput d;
    d.light = DummyLight();
    d.worldPos = posWorld;
    d.worldViewDir = -eyeVec;
    d.atten = atten;
    #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
        d.ambient = 0;
        d.lightmapUV = i_ambientOrLightmapUV;
    #else
        d.ambient = i_ambientOrLightmapUV.rgb;
        d.lightmapUV = 0;
    #endif

    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
        d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
        d.boxMax[0] = unity_SpecCube0_BoxMax;
        d.probePosition[0] = unity_SpecCube0_ProbePosition;
        d.boxMax[1] = unity_SpecCube1_BoxMax;
        d.boxMin[1] = unity_SpecCube1_BoxMin;
        d.probePosition[1] = unity_SpecCube1_ProbePosition;
    #endif

    if(reflections)
    {
        Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, -eyeVec, normalWorld, specColor);
        // Replace the reflUVW if it has been compute in Vertex shader. Note: the compiler will optimize the calcul in UnityGlossyEnvironmentSetup itself
        #if UNITY_STANDARD_SIMPLE
        g.reflUVW = s.reflUVW;
        #endif

        return UnityGlobalIllumination (d, occlusion, normalWorld, g);
    }
    else
    {
        return UnityGlobalIllumination (d, occlusion, normalWorld);
    }
}

