// URP 2D 精灵「轮廓外发光」shader：读取精灵 alpha 轮廓，在轮廓外一圈做光晕。
// 默认「只光晕」模式：本体不渲染（透明），只输出轮廓外一圈光晕 —— 用在人物背后的光晕子对象上，
// 这样人物本身的材质完全不动，光晕是背后单独的一个精灵。
// 原理：顶点阶段把 quad 放大一圈（留出透明边距），片段阶段采样精灵 alpha 求边缘，只在轮廓外渲染光晕色。
Shader "Custom/2D/SpriteOutlineGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Toggle] _OnlyGlow ("只渲染光晕（背后光晕子对象）", Float) = 1
        _OutlineColor ("光晕颜色", Color) = (1, 0.92, 0.4, 1)
        _OutlineWidth ("光晕宽度（膨胀比例）", Range(0, 0.3)) = 0.12
        _GlowStrength ("光晕强度", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteOutlineGlow"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ PIXELSNAP_ON

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            half4 _Color;
            half _OnlyGlow;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _GlowStrength;

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float scale = 1.0 + 2.0 * _OutlineWidth;
                float3 pos = IN.positionOS;
                pos.xy = pos.xy * scale;
                OUT.positionCS = TransformObjectToHClip(pos);

                OUT.uv = IN.uv * scale - _OutlineWidth;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv; // [-width, 1+width]

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv)) * IN.color;
                half inside = (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0) ? 1.0 : 0.0;
                half baseAlpha = baseColor.a * inside;

                float2 t = _MainTex_TexelSize.xy * 2.0;
                half around = 0.0;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2( t.x, 0.0))).a;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(-t.x, 0.0))).a;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(0.0,  t.y))).a;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(0.0, -t.y))).a;
                around = saturate(around - baseAlpha * 4.0);

                half glowAlpha = around * _OutlineColor.a * _GlowStrength;

                if (_OnlyGlow > 0.5)
                {
                    // 只光晕：本体透明，只输出轮廓外一圈光
                    return half4(_OutlineColor.rgb * around * _GlowStrength, glowAlpha);
                }
                else
                {
                    // 本体 + 光晕
                    half4 result = baseColor;
                    result.rgb = baseColor.rgb + _OutlineColor.rgb * around * _GlowStrength;
                    result.a = max(baseAlpha, glowAlpha);
                    return result;
                }
            }
            ENDHLSL
        }
    }
}
