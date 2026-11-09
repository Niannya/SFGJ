// URP 2D 精灵外发光 shader：用于可互动物体的高亮选中。
// 用法：创建一个 Material 引用此 shader，高亮时把 SpriteRenderer.material 换成它。
Shader "Custom/2D/SpriteGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        _GlowColor ("发光颜色", Color) = (1, 0.92, 0.4, 1)
        _GlowStrength ("发光强度", Range(0, 2)) = 1
        _GlowWidth ("发光宽度（像素）", Range(0, 8)) = 2
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
            Name "SpriteGlow"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment

            #pragma multi_compile_local _ PIXELSNAP_ON

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            half4 _Color;
            half4 _GlowColor;
            half _GlowStrength;
            half _GlowWidth;

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

            Varyings SpriteVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 SpriteFragment(Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // 外发光：对四周 alpha 做偏移采样，只在精灵边缘产生光晕
                float2 px = _MainTex_TexelSize.xy * _GlowWidth;
                half around = 0;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( px.x, 0.0)).a;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-px.x, 0.0)).a;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0.0,  px.y)).a;
                around += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0.0, -px.y)).a;

                // 中间点四周也都有 alpha，相减后为 0；只有边缘点才 > 0
                half edge = saturate(around - baseColor.a * 4.0);

                half4 glow = _GlowColor * edge * _GlowStrength;
                half4 result = baseColor + glow;
                result.a = saturate(baseColor.a + edge * _GlowStrength);
                return result;
            }
            ENDHLSL
        }
    }
}
