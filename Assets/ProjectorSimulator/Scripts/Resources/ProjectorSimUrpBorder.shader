Shader "Hidden/ProjectorSimUrpBorder"
{
	Properties
	{
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

			const float rad = 0.5f;

			// draw a white square inside the spotlight circle, our image will sit inside.
			float4 frag(v2f_img i) : COLOR
			{
				const float origin = 0.5f;

				//SUPPOSEDLY WORKING SHADER
				/*
				float u = i.uv[0];
				float v = i.uv[1];

				float a = sqrt((rad*rad*0.5f));

				a = 0.3535533906f; // magic number to fit inside circle, use in other shader!!

				float min = origin - a;
				float max = origin + a;

				if (u > min && u < max && v > min && v < max)
					return float4(1, 1, 1, 1);
				else
					return float4(0, 0, 0, 0);
				*/
			return float4(1, 0, 0, 1);
			}
			ENDCG
		}
	}
}
