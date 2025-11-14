Shader "Custom/InverseBarrel"
{
    Properties
    {
        _MainTex   ("Texture", 2D) = "black" {}
        _Center    ("Center (UV)", Vector) = (0.5, 0.5, 0, 0) // UV in [0..1], per-eye
        _Strength  ("Strength", Float) = 0.15                 // k>0 pincushion (inverse barrel), k<0 barrel
        _ClampBlack("Clamp Outside Black", Float) = 1.0       // 1 = black outside, 0 = wrap sample
        _PreScale  ("Pre-Scale", Float) = 1.15                 // 1.00–1.15 typical
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Cull Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;   // x=1/w, y=1/h, z=w, w=h (Unity fills)
            float2 _Center;              // in UV [0..1] of the source RT
            float  _PreScale;            // overscan pre-zoom factor
            float  _Strength;            // k parameter
            float  _ClampBlack;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize to -1..1 space around _Center with aspect correction
                // (so distortion is radially symmetric even if RT isn't square)
                float2 uv = i.uv;

                // 1) Pre-zoom (overscan) around the distortion center
                uv = (uv - _Center) / _PreScale + _Center;

                // Convert to centered coordinates
                float2 p = uv - _Center;

                // aspect-correct Y to match X scale
                float aspect = _MainTex_TexelSize.w / _MainTex_TexelSize.z; // h/w
                p.y *= aspect;

                // Radial distance squared
                float r2 = dot(p, p);

                // Inverse barrel (pincushion) when _Strength > 0
                // Mapping: p' = p * (1 + k*r^2)
                float k = _Strength;
                float factor = 1.0 + k * r2;
                float2 p2 = p * factor;

                // Undo aspect on Y and shift back to UV
                p2.y /= aspect;
                float2 uv2 = p2 + _Center;

                // Optional clamp outside to black
                if (_ClampBlack > 0.5)
                {
                    if (any(uv2 < 0.0) || any(uv2 > 1.0))
                        return 0;
                }
                else
                {
                    uv2 = frac(uv2);
                }

                return tex2D(_MainTex, uv2);
            }
            ENDCG
        }
    }
}
