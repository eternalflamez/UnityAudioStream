Shader "AVProVideo/Unlit/Opaque (texture+color support) - Android OES ONLY"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_ChromaTex("Chroma", 2D) = "gray" {}
		_Color("Main Color", Color) = (1,1,1,1)

		[KeywordEnum(None, Top_Bottom, Left_Right)] Stereo("Stereo Mode", Float) = 0
		[Toggle(APPLY_GAMMA)] _ApplyGamma("Apply Gamma", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "IgnoreProjector"="False" "Queue"="Geometry" }
		LOD 100
		Lighting Off
		Cull Off

		Pass
		{
			GLSLPROGRAM

			#pragma only_renderers gles gles3
			#extension GL_OES_EGL_image_external : require
			#extension GL_OES_EGL_image_external_essl3 : enable
			
			precision mediump float;

			#ifdef VERTEX

			#include "UnityCG.glslinc"
			#define SHADERLAB_GLSL
			#include "AVProVideo.cginc"
		
			varying vec2 texVal;
			uniform vec4 _MainTex_ST;

			void main()
			{
				gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				texVal = TRANSFORM_TEX(gl_MultiTexCoord0.xy, _MainTex);
				//texVal.x = 1.0 - texVal.x;
				texVal.y = 1.0 - texVal.y;
            }
            #endif  

			#ifdef FRAGMENT

			varying vec2 texVal;

			uniform samplerExternalOES _MainTex;

            void main()
            {          
#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
				gl_FragColor = texture2D(_MainTex, texVal.xy);
#else
				gl_FragColor = vec4(1.0, 1.0, 0.0, 1.0);
#endif
			}
            #endif       
				
			ENDGLSL
		}
	}
	
	Fallback "AVProVideo/Unlit/Opaque (texture+color+fog+stereo support)"
}