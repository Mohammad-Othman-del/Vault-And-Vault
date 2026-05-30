Shader "Custom/GridWall"
{
    Properties
    {
        _WallBase     ("Base Color",     Color) = (0.02, 0.01, 0.05, 1)
        _WallGrid     ("Grid Color",     Color) = (1, 0.05, 0.05, 1)
        _GridScale    ("Grid Scale",     Float) = 15
        _LineWidth    ("Line Width",     Float) = 0.025
        _GlowWidth    ("Glow Width",     Float) = 0.18
        _GlowIntensity("Glow Intensity", Float) = 1.8
        _PulseSpeed   ("Pulse Speed",    Float) = 1.5
        _PulseAmount  ("Pulse Amount",   Float) = 0.18
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float4 _WallBase;
            float4 _WallGrid;
            float  _GridScale;
            float  _LineWidth;
            float  _GlowWidth;
            float  _GlowIntensity;
            float  _PulseSpeed;
            float  _PulseAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * _GridScale;
                float2 f  = frac(uv);

                float2 d = min(f, 1.0 - f);
                float  closest = min(d.x, d.y);

                float gridLine = 1.0 - smoothstep(0.0, _LineWidth, closest);
                float glow     = (1.0 - smoothstep(0.0, _GlowWidth, closest)) * _GlowIntensity;
                float pulse    = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);

                float4 col = _WallBase + _WallGrid * (gridLine + glow * pulse);
                return col;
            }
            ENDCG
        }
    }
}
