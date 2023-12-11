Shader "Custom/UI_GrabBlurTexture"
{
	Properties
	{
		_MainTex ("Main Tex", 2D) = "white" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

	struct appdata
	{
		half4 vertex : POSITION;
		half4 color : COLOR;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f
	{
		half4 vertex : SV_POSITION;
		half4 screenPos : TEXCOORD0;
		half4 color : COLOR;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	//sampler2D _GrabBlurTex;
	//sampler2D _GrabTex;
	TEXTURE2D_X(_GrabTex);
	TEXTURE2D_X(_GrabBlurTex);
	//SAMPLER(sampler_GrabBlurTex);
	
	v2f vert (appdata v)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		o.vertex = TransformObjectToHClip(v.vertex.xyz);
		o.screenPos = ComputeScreenPos(o.vertex);
		o.color = v.color;
		return o;
	}
	
	half4 frag (v2f input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		float2 screenUV = input.vertex.xy * rcp(_ScreenParams.xy);
		half4 color1 = SAMPLE_TEXTURE2D_X(_GrabTex, sampler_LinearRepeat, screenUV);
		half4 color2 = SAMPLE_TEXTURE2D_X(_GrabBlurTex, sampler_LinearRepeat, screenUV);
		return lerp(color1, color2, input.color.a);
		//return lerp(tex2Dproj(_GrabTex, input.screenPos), tex2Dproj(_GrabBlurTex, input.screenPos), input.color.a);
	}

	ENDHLSL

	SubShader
	{
		LOD 100
		Cull Back
        ZWrite Off

		Tags { "RenderType"="Transparent" "Queue" = "Transparent" "PreviewType"="Plane" "RenderPipeline" = "UniversalPipeline"}
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}
}