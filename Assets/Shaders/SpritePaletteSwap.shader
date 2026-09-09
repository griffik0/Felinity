Shader "Felinity/SpritePaletteSwap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _KeyColor0 ("Key Color 0", Color) = (0,0,0,0)
        _ReplaceColor0 ("Replace Color 0", Color) = (0,0,0,0)
        _KeyColor1 ("Key Color 1", Color) = (0,0,0,0)
        _ReplaceColor1 ("Replace Color 1", Color) = (0,0,0,0)
        _KeyColor2 ("Key Color 2", Color) = (0,0,0,0)
        _ReplaceColor2 ("Replace Color 2", Color) = (0,0,0,0)
        _KeyColor3 ("Key Color 3", Color) = (0,0,0,0)
        _ReplaceColor3 ("Replace Color 3", Color) = (0,0,0,0)

        _Tolerance ("Match Tolerance", Range(0.001, 0.2)) = 0.05

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "SpritePaletteSwap"
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;

            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;

            fixed4 _KeyColor0;
            fixed4 _ReplaceColor0;
            fixed4 _KeyColor1;
            fixed4 _ReplaceColor1;
            fixed4 _KeyColor2;
            fixed4 _ReplaceColor2;
            fixed4 _KeyColor3;
            fixed4 _ReplaceColor3;
            float _Tolerance;

            inline float4 UnityFlipSprite(in float3 pos, in float2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex.xyz, _Flip.xy);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            bool MatchesKey(fixed3 rgb, fixed4 key)
            {
                // Unused keys (alpha ~ 0) never match.
                if (key.a < 0.01)
                    return false;
                return distance(rgb, key.rgb) <= _Tolerance;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord);

                if (MatchesKey(c.rgb, _KeyColor0))
                    c.rgb = _ReplaceColor0.rgb;
                else if (MatchesKey(c.rgb, _KeyColor1))
                    c.rgb = _ReplaceColor1.rgb;
                else if (MatchesKey(c.rgb, _KeyColor2))
                    c.rgb = _ReplaceColor2.rgb;
                else if (MatchesKey(c.rgb, _KeyColor3))
                    c.rgb = _ReplaceColor3.rgb;

                c *= IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
