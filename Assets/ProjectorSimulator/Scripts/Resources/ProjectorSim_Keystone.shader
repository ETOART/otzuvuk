Shader "Hidden/ProjectorSimKeystone"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Channel("Channel Index", int) = 0
	}

		SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			//#pragma enable_d3d11_debug_symbols
			#include "UnityCG.cginc"
			uniform sampler2D _MainTex;
			
			float _keystoneH;
			float _keystoneV;

			float lerp(float y0, float y1, float t) {
				return y0 + t * (y1 - y0);
			}

			float4 frag(v2f_img i) : COLOR
			{
				float u = i.uv[0];
				float v = i.uv[1];

				float keystoneMinWidth  = 1.0f - abs(_keystoneV);
				float keystoneMinHeight = 1.0f - abs(_keystoneH);

				float colHeight = lerp(1, keystoneMinHeight, _keystoneH > 0 ? u : 1 - u);
				float rowWidth  = lerp(1, keystoneMinWidth,  _keystoneV > 0 ? v : 1 - v);

				float rowProgress = (u - (0.5f - rowWidth  / 2)) / rowWidth;
				float colProgress = (v - (0.5f - colHeight / 2)) / colHeight;

				float4 c;

				if (rowProgress >= 0 && rowProgress <= 1 && colProgress >= 0 && colProgress <= 1)
				{
					// get pixel colour from projected image
					c = tex2D(_MainTex, half2(rowProgress, colProgress));
				}
				else
				{
					// black
					c = float4(0, 0, 0, 1);
				}
				return c;
			}
			ENDCG
		}
	}
}
