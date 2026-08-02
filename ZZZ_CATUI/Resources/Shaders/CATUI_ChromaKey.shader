Shader "CATUI/ChromaKey"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _KeyColor ("Key Color", Color) = (0,0,0,1)
        _Tolerance ("Tolerance", Float) = 0.15
        _Smoothing ("Smoothing", Float) = 0.1
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile ______ USE_NOISE_TEXTURE
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _KeyColor;
            float _Tolerance;
            float _Smoothing;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // Calculate color distance to key color
                float dist = distance(col.rgb, _KeyColor.rgb);
                
                // Apply chroma key with smooth transition
                float alpha;
                if (dist <= _Tolerance)
                {
                    alpha = 0.0;
                }
                else if (dist >= _Tolerance + _Smoothing)
                {
                    alpha = 1.0;
                }
                else
                {
                    alpha = smoothstep(_Tolerance, _Tolerance + _Smoothing, dist);
                }
                
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}