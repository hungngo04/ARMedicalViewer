Shader "Custom/3DTextureSlicer"
{
    Properties
    {
        _VolumeTex("Volume Texture", 3D) = "" {}
        _SlicePos("Slice Position", Float) = 0.5
        _SliceAxis("Slice Axis", Vector) = (1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler3D _VolumeTex;
            float _SlicePos;
            float3 _SliceAxis;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 coord;

                // Determine slicing direction
                if (_SliceAxis.x == 1) // Sagittal (YZ Plane)
                {
                    coord = float3(_SlicePos, i.uv.x, i.uv.y);
                }
                else if (_SliceAxis.y == 1) // Coronal (XZ Plane)
                {
                    coord = float3(i.uv.x, _SlicePos, i.uv.y);
                }
                else if (_SliceAxis.z == 1) // Axial (XY Plane)
                {
                    coord = float3(i.uv.x, i.uv.y, _SlicePos);
                }
                else
                {
                    coord = float3(0.0, 0.0, 0.0); // Default fallback
                }

                return tex3D(_VolumeTex, coord);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
