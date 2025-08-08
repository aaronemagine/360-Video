Shader "Custom/Floor"
{
    Properties
    {
        _TopColor("Top Color", Color) = (1, 0.3, 0.3, 1)
        _MiddleColor("MiddleColor", Color) = (1.0, 1.0, 1)
        _BottomColor("Bottom Color", Color) = (0.3, 0.3, 1, 1)
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Direction("Direction", Vector) = (0, 1, 0)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
            half3 _TopColor;
            half3 _BottomColor;
            half3 _MiddleColor;
            float3 _Direction;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {

            float3 texcoord = -normalize(IN.viewDir);
            //half ditherNoise = DitherAnimatedNoise(i.vertex.xy);

            float range = dot(texcoord, _Direction);// +ditherNoise;

            half bottomRange = saturate(-range);
            half middleRange = 1 - abs(range);
            half topRange = saturate(range);

            half3 finalColor = _BottomColor.rgb * bottomRange
                + _MiddleColor.rgb * middleRange
                + _TopColor.rgb * topRange;

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = finalColor * _Color;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;// _Metallic;
            o.Smoothness = 0;// _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
