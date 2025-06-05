Shader "Custom/BarrelDistortion"
{
    // This is a custom made shader for supressing barell distortion 
    // caused by used lenses. The lenses used are from early version 
    // of Samsung Gear VR and are perfectly spherical. This results 
    // in a relatively simple inverse pre-distorion using barrel stretch.


    Properties
    {
        // Image to distort
        _MainTex ("Texture", 2D) = "white" {}
        // Distortion power
        _Distortion ("Distortion Strength", Float) = 0.3
    }
    SubShader
    {
        // Shader is opaque
        Tags { "RenderType"="Opaque" }
        // Set maximum Level Of Detail
        LOD 100

        Pass
        {
            // Custom vertex and fragment function
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Import predefined variables
            sampler2D _MainTex;
            float _Distortion;

            struct v2f {
                // Vertex-to-fragment

                // Screen-space position of the pixel
                float4 pos : SV_POSITION;
                // Texture coordinate
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                // Converts vertex space object to plane object

                // Create a new instance
                v2f o;
                // Changes 3D world to a 2D screen
                o.pos = UnityObjectToClipPos(v.vertex);
                // Expand the UV from [0,1] to [-1,1], so that centre is 0
                o.uv = v.texcoord.xy * 2.0 - 1.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Applies distortion

                // Copy centred UV
                float2 uv = i.uv;
                // Measure the distance from origin
                float r2 = dot(uv, uv);
                // Aply distortion
                float2 distortedUV = uv * (1 + _Distortion * r2);
                // Convert UV back to default range
                distortedUV = (distortedUV + 1.0) * 0.5;
                return tex2D(_MainTex, distortedUV);
            }
            ENDCG
        }
    }
}
