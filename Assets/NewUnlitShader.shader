Shader "Unlit/SH4"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_NormalTex("NormalTex", 2D) = "white" {}
		_SH4CoffTexR("SH4CoffTexR", 2D) = "white" {}
		_SH4CoffTexG("SH4CoffTexG", 2D) = "white" {}
		_SH4CoffTexB("SH4CoffTexB", 2D) = "white" {}
		_BakeDiffuse("Bake Diffuse", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float4 lightmap_uv : TEXCOORD1;				
				fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				//float3 tangent_ws : TEXCOORD2;
				//float3 binormal_ws : TEXCOORD3;
				float4 lightmap_uv : TEXCOORD4;
            };

			struct SH4Color
			{
				float4 c[3];
			};

            sampler2D _MainTex;
            float4 _MainTex_ST;
			sampler2D _NormalTex;
			float4 _NormalTex_ST;
			sampler2D _SH4CoffTexR;
			float4 _SH4CoffTexR_ST;
			sampler2D _SH4CoffTexG;
			float4 _SH4CoffTexG_ST;
			sampler2D _SH4CoffTexB;
			float4 _SH4CoffTexB_ST;
			sampler2D _BakeDiffuse;
			float4 _BakeDiffuse_ST;

			SH4Color ProjectOntoSH4Color(float3 dir, float3 color, float A0, float A1)
			{
				SH4Color sh;

				float c0 = 0.282095f * A0;
				float c1 = -0.488603f * dir.y * A1;
				float c2 = 0.488603f * dir.z * A1;
				float c3 = -0.488603f * dir.x * A1;

				float4 sh_cof = float4(c0, c1, c2, c3);

				sh.c[0] = color.r * sh_cof;
				sh.c[1] = color.g * sh_cof;
				sh.c[2] = color.b * sh_cof;

				//// Band 0
				//sh.c[0] = 0.282095f * A0 * color;

				//// Band 1
				//sh.c[1] = -0.488603f * dir.y * A1 * color;
				//sh.c[2] = 0.488603f * dir.z * A1 * color;
				//sh.c[3] = -0.488603f * dir.x * A1 * color;

				return sh;
			}

			float3 SHDotProduct(in SH4Color a, in SH4Color b)
			{
				float3 result = 0.0f;

				/*for (uint i = 0; i < 4; ++i)
					result += a.c[i] * b.c[i];*/

				for (uint i = 0; i < 3; ++i)
				{
					result += float3(a.c[0][i] * b.c[0][i], a.c[1][i] * b.c[1][i], a.c[2][i] * b.c[2][i]);
				}

				return result;
			}

			float3 EvalSH4Irradiance(float3 dir, SH4Color sh_color)
			{
				float Pi = 3.141592f;
				float CosineA0 = Pi;
				float CosineA1 = (2.0f * Pi) / 3.0f;

				SH4Color dirSH = ProjectOntoSH4Color(dir, 1.0f, CosineA0, CosineA1);
				return SHDotProduct(dirSH, sh_color);
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

				//float3 binormal = cross(normalize(v.normal), normalize(v.tangent.xyz)) * v.tangent.w;

				o.lightmap_uv = v.lightmap_uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) / 3.14f;
			
				SH4Color sh_color;
				sh_color.c[0] = tex2D(_SH4CoffTexR, i.lightmap_uv);
				sh_color.c[1] = tex2D(_SH4CoffTexG, i.lightmap_uv);
				sh_color.c[2] = tex2D(_SH4CoffTexB, i.lightmap_uv);

				float3 normal_ts = normalize(tex2D(_NormalTex, float2(i.uv.x, 1 - i.uv.y)).xyz * 2 - 1);

				float3 irradiance = EvalSH4Irradiance(normal_ts, sh_color);
				col.rgb += irradiance;

				float4 bake_diffuse = tex2D(_BakeDiffuse, i.lightmap_uv);
				//float4 bake_diffuse = tex2D(_BakeDiffuse, float2(0.06f, 0.5f));

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
				//return float4(irradiance, 1.0);
				//return bake_diffuse;
            }
            ENDCG
        }
    }
}
