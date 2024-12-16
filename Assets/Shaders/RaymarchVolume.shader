// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/RaymarchVolume"
{
    Properties
    {
        _VolumeTex("Volume Texture", 3D) = "" {}
        _Steps("Ray Steps", Range(8,512)) = 128
        _IntensityScale("Intensity Scale", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX3D(_VolumeTex);

            float _IntensityScale;
            int _Steps;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float4 objPos = mul(unity_ObjectToWorld, v.vertex);
                float3 camPos = mul(unity_WorldToObject, _WorldSpaceCameraPos);
                o.rayOrigin = camPos;
                o.rayDir = normalize(objPos.xyz - camPos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3(0.5, 0.5, 0.5);

                float3 t1 = (boxMin - i.rayOrigin) / i.rayDir;
                float3 t2 = (boxMax - i.rayOrigin) / i.rayDir;
                float3 tmin = min(t1, t2);
                float3 tmax = max(t1, t2);

                float entry = max(max(tmin.x, tmin.y), tmin.z);
                float exit = min(min(tmax.x, tmax.y), tmax.z);

                if (exit < 0.0 || entry > exit)
                    return float4(0,0,0,0);

                entry = max(entry, 0.0);
                float3 rayStep = i.rayDir * ((exit - entry) / _Steps);
                float3 samplePos = i.rayOrigin + i.rayDir * entry;
                float4 accumulatedColor = float4(0,0,0,0);

                for (int step = 0; step < _Steps; step++)
                {
                    // Convert from [-0.5, 0.5] to [0,1]
                    float3 uvw = samplePos + 0.5;
                    float4 voxelColor = UNITY_SAMPLE_TEX3D(_VolumeTex, uvw);

                    float intensity = voxelColor.r * _IntensityScale;
                    float opacity = intensity;
                    float3 color = float3(intensity, intensity, intensity);

                    float srcAlpha = opacity * (1.0 - accumulatedColor.a);
                    accumulatedColor.rgb += color * srcAlpha;
                    accumulatedColor.a += srcAlpha;

                    if (accumulatedColor.a >= 1.0)
                        break;

                    samplePos += rayStep;
                }

                return accumulatedColor;
            }
            ENDCG
        }
    }
    Fallback "Transparent"
}
