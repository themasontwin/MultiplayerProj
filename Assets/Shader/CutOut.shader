Shader "Unlit/CutOut"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HolePos1("Hole Position 1", Vector) = (0,0,0,0)
        _HoleRad1("Hole Radius 1", Float) = 1.0
        _HolePos2("Hole Position 2", Vector) = (0,0,0,0)
        _HoleRad2("Hole Radius 2", Float) = 1.0
        _Color ("Base Color", Color) = (1,1,1,1)
        _RaiseHeight("Raise Height", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="Transparent" }
        LOD 100
        Cull Off // Renders backface, fully hollow otherwise

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            float3 _HolePos1;
            float _HoleRad1;
            float3 _HolePos2;
            float _HoleRad2;


            float _RaiseHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                // Local vertex pos to world space
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Distance from hole's center
                float first_dist = distance(i.worldPos.xz, _HolePos1.xz);
                float second_dist = distance(i.worldPos.xz, _HolePos2.xz);

                if (first_dist < _HoleRad1 || second_dist < _HoleRad2) {
                    discard;
                }



                fixed4 col = tex2D(_MainTex, i.uv);
                
                return col * _Color;
            }
            ENDCG
        }
    }
}
