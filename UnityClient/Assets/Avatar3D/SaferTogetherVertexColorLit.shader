Shader "SaferTogether/VertexColorLit"
{
    // Quaternius low-poly characters store their colors in MESH VERTEX COLORS and ship no
    // textures. URP/Lit ignores vertex colors (plain white) and Built-in Standard is magenta
    // under URP, so this is a minimal URP UNLIT vertex-color shader.
    //
    // The pass MUST tag LightMode = "UniversalForward" or URP finds no renderable pass and shows
    // magenta (with no compile error). The body is kept minimal (no extra #pragma target / no
    // ternaries) so it cross-compiles cleanly to WebGL (GLES3). FallBack guarantees it can never
    // render magenta even if the SubShader is somehow unsupported.
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return half4(IN.color.rgb * _Tint.rgb, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
