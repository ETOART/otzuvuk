Shader "Hidden/ProjectorSimSinglePass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

		SubShader
	{
		Pass
		{
			ZTest Always // required for WebGL

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			uniform sampler2D _MainTex;
			uniform float4 _PJSimTransform;
			uniform int _PJSimCookieSize;

			float4 frag(v2f_img i) : COLOR
			{
				float scale = 2;
				float offset = 0.5;
				float u = _PJSimTransform.z + (i.uv[0] * _PJSimTransform.x);
				float v = _PJSimTransform.w + (i.uv[1] * _PJSimTransform.y);
				float borderSize = 8.0 / (float)_PJSimCookieSize; // bigger black border so it is still present when we scale down the image for the URP spotlight
				if (u <= borderSize || u >= 1 - borderSize || v <= borderSize || v >= 1 - borderSize)
					return float4(0, 0, 0, 0);
				return tex2D(_MainTex, half2(u,v));
			}
			ENDCG
		}
	}
}
