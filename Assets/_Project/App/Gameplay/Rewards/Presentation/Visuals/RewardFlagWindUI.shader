Shader "ForceOfNature/UI/Reward Flag Wind"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 0.02)) = 0.004
        _WindSpeed ("Wind Speed", Range(0, 6)) = 1.15
        _WaveFrequency ("Wave Frequency", Range(0, 30)) = 10
        _SecondaryStrength ("Secondary Strength", Range(0, 1)) = 0.35
        _SecondaryFrequency ("Secondary Frequency", Range(0, 40)) = 18
        _VerticalRippleStrength ("Vertical Ripple Strength", Range(0, 0.01)) = 0.001
        _ShadingStrength ("Shading Strength", Range(0, 0.2)) = 0.06

        [Header(Area)]
        _RectHeight ("Rect Height", Float) = 800
        _BottomInfluence ("Bottom Influence", Range(0.01, 1)) = 0.7
        _FalloffPower ("Falloff Power", Range(0.25, 4)) = 1.2

        [Header(Phase)]
        _PhaseOffset ("Phase Offset", Range(0, 6.283185)) = 0
        _CanvasPhaseScale ("Canvas Phase Scale", Range(0, 0.1)) = 0.015

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPosition : TEXCOORD2;
                float2 canvasPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _WindStrength;
            float _WindSpeed;
            float _WaveFrequency;
            float _SecondaryStrength;
            float _SecondaryFrequency;
            float _VerticalRippleStrength;
            float _ShadingStrength;
            float _RectHeight;
            float _BottomInfluence;
            float _FalloffPower;
            float _PhaseOffset;
            float _CanvasPhaseScale;
            float _RewardFlagWindTime;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.localPosition = IN.vertex.xy;
                OUT.canvasPosition = mul(unity_ObjectToWorld, IN.vertex).xy;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float rectHeight = max(abs(_RectHeight), 0.001);
                float y01 = saturate(IN.localPosition.y / rectHeight + 0.5);
                float influence = saturate(_BottomInfluence);
                float fadeWidth = max(1.0 - influence, 0.001);
                float fade = saturate((y01 - influence) / fadeWidth);
                float smoothFade = fade * fade * (3.0 - 2.0 * fade);
                float bottomMask = 1.0 - smoothFade;
                bottomMask = pow(saturate(bottomMask), _FalloffPower);

                float shaderTime = lerp(_Time.y, _RewardFlagWindTime, step(0.0001, abs(_RewardFlagWindTime)));
                float time = shaderTime * _WindSpeed + _PhaseOffset + IN.canvasPosition.x * _CanvasPhaseScale;
                float primaryWave = sin(IN.localPosition.x * _WaveFrequency * 0.01 + time);
                float secondaryWave = sin(
                    (IN.localPosition.x * _SecondaryFrequency + IN.localPosition.y * 0.35) * 0.01 - time * 1.31
                ) * _SecondaryStrength;

                float windOffset = (primaryWave + secondaryWave) * _WindStrength * bottomMask;
                float verticalOffset = sin(IN.localPosition.x * _WaveFrequency * 0.007 + time * 0.75)
                    * _VerticalRippleStrength
                    * bottomMask;

                float2 warpedUv = IN.texcoord + float2(windOffset, verticalOffset);
                fixed4 baseColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                fixed4 windColor = tex2D(_MainTex, warpedUv) + _TextureSampleAdd;
                float shading = (primaryWave * 0.6 + secondaryWave * 0.4) * _ShadingStrength * bottomMask;
                fixed4 color = fixed4(windColor.rgb * max(0.0, 1.0 + shading), baseColor.a) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
