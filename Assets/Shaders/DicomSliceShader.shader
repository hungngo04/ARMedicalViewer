Shader "Custom/DicomSliceShader"
{
    Properties
    {
        _VolumeTex ("Volume Texture", 3D) = "" {}
        _SliceIndex ("Slice Index", Float) = 0
        _SliceDirection ("Slice Direction (0=Axial, 1=Sagittal, 2=Coronal)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler3D _VolumeTex;
            float _SliceIndex;
            float _SliceDirection;
            float _VolumeWidth;
            float _VolumeHeight;
            float _VolumeDepth;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 texCoords;
                float sliceIndexNormalized;

                if (_SliceDirection == 0) // Axial
                {
                    sliceIndexNormalized = _SliceIndex / (_VolumeDepth - 1);
                    texCoords = float3(i.uv.x, i.uv.y, sliceIndexNormalized);
                }
                else if (_SliceDirection == 1) // Sagittal
                {
                    sliceIndexNormalized = _SliceIndex / (_VolumeWidth - 1);
                    texCoords = float3(sliceIndexNormalized, i.uv.y, i.uv.x);
                }
                else if (_SliceDirection == 2) // Coronal
                {
                    sliceIndexNormalized = _SliceIndex / (_VolumeHeight - 1);
                    texCoords = float3(i.uv.x, sliceIndexNormalized, i.uv.y);
                }
                else
                {
                    texCoords = float3(i.uv, 0.0);
                }

                return tex3D(_VolumeTex, texCoords);
            }
            ENDCG
        }
    }
}
