// Help from: https://jov.arvojournals.org/article.aspx?articleid=2627514

Shader "Custom/Wetness" 
{
    Properties
    {
        // Color tint for the object.
        _Color ("Color", Color) = (1,1,1,1)
        
        // Base Albedo Texture
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        // Normal Map
        _BumpMap("Normal Map", 2D) = "bump" {}
        
        // Basic Metallic & Glossiness Controls
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        // HSL Tweaks for the "wet" look
        _Saturation("Extra Saturation", Range(0,0.2)) = 0.1
        _Lightness("Lightness Factor", Range(0.1,1)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        // Samplers for our textures
        sampler2D _MainTex;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
        };

        // These variables are exposed to the editor via Properties
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _Saturation;
        half _Lightness;

        half3 RGBtoHSL(half3 rgbColor)
        {
            // Extract RGB channels into single variables
            half red   = rgbColor.r;
            half green = rgbColor.g;
            half blue  = rgbColor.b;

            // Determine min and max channels to help calculate HSL
            half maxChannel = max(max(red, green), blue);
            half minChannel = min(min(red, green), blue);
            half delta      = maxChannel - minChannel;

            // Initialize HSL components
            half hue        = 0;
            half saturation = 0;
            half lightness  = (maxChannel + minChannel) * 0.5;

            // If delta is super tiny, the color is near grayscale => hue & saturation ~ 0
            if (delta < 1e-5)
            {
                hue = 0;
                saturation = 0;
            }
            else
            {
                // Saturation formula
                saturation = delta / (1 - abs(2 * lightness - 1));

                // Hue formula depends on which channel is max
                     if (maxChannel == red)   hue = 60 * ((green - blue) / delta + (green < blue ? 6 : 0));
                else if (maxChannel == green) hue = 60 * ((blue - red)   / delta + 2);
                else                          hue = 60 * ((red - green)   / delta + 4);
            }
            // Normalize hue to [0..1] instead of [0..360]
            hue /= 360;

            return half3(hue, saturation, lightness);
        }

        half3 HSLtoRGB(half3 hslColor)
        {
            // Grab the individual components from the HSL vector
            half H = hslColor.r; 
            half S = hslColor.g;  
            half L = hslColor.b;  

            // Convert hue to degrees temporarily
            half hueDegrees = H * 360.0;

            // This is basically the standard formula for going from HSL to RGB
            half C = (1 - abs(2 * L - 1)) * S;
            half X = C * (1 - abs(fmod(hueDegrees / 60.0, 2.0) - 1));
            half m = L - C * 0.5;

            half3 rgbPrim;
            if      (hueDegrees < 60)   rgbPrim = half3(C, X, 0);
            else if (hueDegrees < 120)  rgbPrim = half3(X, C, 0);
            else if (hueDegrees < 180)  rgbPrim = half3(0, C, X);
            else if (hueDegrees < 240)  rgbPrim = half3(0, X, C);
            else if (hueDegrees < 300)  rgbPrim = half3(X, 0, C);
            else                        rgbPrim = half3(C, 0, X);

            return rgbPrim + m;
        }

        half3 Wetter(half3 color)
        {
            // Convert to HSL for intuitive modification
            half3 hslColor = RGBtoHSL(color);

            // Increase saturation by user-defined amount
            hslColor.g += _Saturation; 

            // Nonlinear approach: multiply L by L, then scale by user param
            hslColor.b = hslColor.b * hslColor.b * _Lightness; 

            // Convert back to RGB
            return HSLtoRGB(hslColor);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample the base texture and tint it with _Color
            fixed4 sampledColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Always transform the sampled color to its “wet” version
            half3 finalColor = Wetter(sampledColor.rgb);

            // Write out the results to the surface output
            o.Albedo     = finalColor;
            o.Metallic   = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha      = sampledColor.a;
            o.Normal     = tex2D(_BumpMap, IN.uv_MainTex).rgb;
        }
        ENDCG
    }

    FallBack "Diffuse"
}


