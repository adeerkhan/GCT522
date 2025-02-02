// Edited the Toon Shader from Task-1, added wind effects
// Trees in the Wind - with Unity Shader Graph (https://www.youtube.com/watch?v=EBADOmohQ8M)
// Reversed the Shader Graph from video to shader code by combining with my Task-1 Toon Shader

Shader "Toon/Plants&Trees"
{
    Properties
    {
        // Main Rendering Properties
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        
        // Rim Lighting Properties
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(1.0, 10.0)) = 3.0
        _RimThreshold ("Rim Threshold", Range(0.0, 1.0)) = 0.1 // For rim modulation

        // Toon Shading Properties
        _Threshold ("Threshold", Range(1.0, 10.0)) = 5
        _ShadeColor ("Shade Color", Color) = (0.2,0.2,0.2,1)

        // Outline Properties
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.005

        // Lighting Properties
        _AmbientColor ("Ambient Color", Color) = (0.2,0.2,0.2,1)
        _SpecularColor ("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness ("Glossiness", Float) = 32

        // Wind Properties
        _WindAmplitude ("Wind Amplitude", Float) = 1.5 // Controls the maximum displacement amplitude
        _WindAmplitudeOffset ("Wind Amplitude Offset", Float) = 2.0 // Controls the offset for amplitude variation
        _WindFrequency ("Wind Frequency", Float) = 1.11 // Controls the frequency of wind oscillations
        _WindFrequencyOffset ("Wind Frequency Offset", Float) = 0.0 // Offset for frequency modulation
        _WindPhase ("Wind Phase", Float) = 1.0 // Phase shift for wind oscillations
        _WindDir ("Wind Direction", Range(0, 360)) = 0.0 // Base direction of wind in degrees
        _WindDirOffset ("Wind Direction Offset", Range(0, 180)) = 20.0 // Offset for wind direction variation
        _WindMaxHeight ("Max Height for Bending", Float) = 10.0 // Maximum height for applying wind displacement
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        // --- Outline Pass ---
        Pass
        {
            ZWrite On
            Cull Front

            CGPROGRAM
            #pragma vertex vert_outline
            #pragma fragment frag_outline
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            // Wind Properties (must match the main pass)
            float _WindAmplitude;
            float _WindAmplitudeOffset;
            float _WindFrequency;
            float _WindFrequencyOffset;
            float _WindPhase;
            float _WindDir;
            float _WindDirOffset;
            float _WindMaxHeight;

            v2f vert_outline (appdata v)
            {
                v2f o;

                /********************************************/
                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */

                // Procedural Wind Displacement
                // Convert wind direction from degrees to radians
                float windDirRad = radians(_WindDir + _WindDirOffset * (0.5 - sin(_Time.y * 0.1)));

                // Calculate wind influence based on position and time
                // Use sine wave for simple wind animation
                float windEffect = sin((_WindFrequency + _WindFrequencyOffset) * v.vertex.x + _Time.y * _WindPhase);

                // Calculate displacement amplitude
                float amplitude = _WindAmplitude + _WindAmplitudeOffset * sin(_Time.y * 0.5);

                // Calculate displacement based on wind direction
                float3 windDir = float3(cos(windDirRad), 0, sin(windDirRad));
                float3 displacement = windDir * windEffect * amplitude;

                // Apply displacement only to vertices below MaxHeight
                float heightFactor = saturate(v.vertex.y / _WindMaxHeight);
                displacement *= heightFactor;

                // Modify the vertex position with displacement
                float4 displacedVertex = v.vertex + float4(displacement, 0.0);

                // Transform the displaced vertex to clip space
                float4 clipPos = UnityObjectToClipPos(displacedVertex);

                // Offset the position along the normal for the outline
                float3 normalWorld = UnityObjectToWorldNormal(v.normal);
                float3 posWorld = mul(unity_ObjectToWorld, displacedVertex).xyz;
                posWorld += normalWorld * _OutlineWidth;

                // Transform the offset position back to clip space
                o.pos = UnityWorldToClipPos(float4(posWorld, 1.0));

                // Set the outline color
                o.color = _OutlineColor;

                /********************************************/
                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */

                return o;
            }

            fixed4 frag_outline (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }

        // --- Main Rendering Pass ---
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert_main
            #pragma fragment frag_main
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 lightDir : TEXCOORD3;
                // For shadow mapping if needed
                SHADOW_COORDS(4)
            };

            // Texture Properties
            sampler2D _MainTex;
            float4 _MainTex_ST;

            // Color Properties
            float4 _MainColor;
            float4 _ShadeColor;
            float4 _AmbientColor;
            float4 _SpecularColor;
            float _Glossiness;

            // Rim Properties
            float4 _RimColor;
            float _RimPower;
            float _RimThreshold; // For rim modulation

            // Toon Properties
            float _Threshold;

            // Wind Properties
            float _WindAmplitude;
            float _WindAmplitudeOffset;
            float _WindFrequency;
            float _WindFrequencyOffset;
            float _WindPhase;
            float _WindDir;
            float _WindDirOffset;
            float _WindMaxHeight;

            v2f vert_main (appdata v)
            {
                v2f o;

                /********************************************/
                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */

                // Procedural Wind Displacement
                // Convert wind direction from degrees to radians
                float windDirRad = radians(_WindDir + _WindDirOffset * (0.5 - sin(_Time.y * 0.1)));

                // Calculate wind influence based on position and time
                // Use sine wave for simple wind animation
                float windEffect = sin((_WindFrequency + _WindFrequencyOffset) * v.vertex.x + _Time.y * _WindPhase);

                // Calculate displacement amplitude
                float amplitude = _WindAmplitude + _WindAmplitudeOffset * sin(_Time.y * 0.5);

                // Calculate displacement based on wind direction
                float3 windDir = float3(cos(windDirRad), 0, sin(windDirRad));
                float3 displacement = windDir * windEffect * amplitude;

                // Apply displacement only to vertices below MaxHeight
                float heightFactor = saturate(v.vertex.y / _WindMaxHeight);
                displacement *= heightFactor;

                // Modify the vertex position with displacement
                float4 displacedVertex = v.vertex + float4(displacement, 0.0);

                // Transform the displaced vertex to clip space
                o.pos = UnityObjectToClipPos(displacedVertex);

                // Normalize and transform the normal to world space
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));

                // Pass through UV coordinates
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Compute world position
                float3 worldPos = mul(unity_ObjectToWorld, displacedVertex).xyz;

                // Compute the view direction (from surface to camera)
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // Compute the light direction (from surface to light)
                o.lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Transfer shadow coordinates if using shadows
                TRANSFER_SHADOW(o)

                /********************************************/
                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */

                return o;
            }

            fixed4 frag_main (v2f i) : SV_Target
            {
                // Sample the main texture
                float4 texColor = tex2D(_MainTex, i.uv) * _MainColor;

                // Initialize the final color with ambient lighting
                float3 ambient = _AmbientColor.rgb * texColor.rgb;

                // Task 1-1. Implement the Rim Shader (2 points)

                /********************************************/
                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */

                // Calculate NdotL for rim modulation
                float NdotL = saturate(dot(normalize(i.normal), normalize(i.lightDir)));

                // Calculate rim dot based on view direction and normal
                float rimDot = 1.0 - saturate(dot(normalize(i.normal), normalize(i.viewDir)));

                // Calculate rim intensity, modulated by NdotL
                float rimIntensity = rimDot * NdotL;

                // Apply pow function to scale the rim
                rimIntensity = pow(rimIntensity, _RimPower);

                // Apply smoothstep for smoother blending
                rimIntensity = smoothstep(_RimThreshold - 0.01, _RimThreshold + 0.01, rimIntensity);

                // Final rim color
                float3 rimColor = rimIntensity * _RimColor.rgb;

                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */
                /********************************************/

                // Task 1-3. Implement the Toon Shader (3 points)

                /********************************************/
                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */

                // Apply toon step
                float toonStep = floor(NdotL * _Threshold) / _Threshold;
                float3 toonColor = lerp(_ShadeColor.rgb, texColor.rgb, toonStep);

                /* DO NOT MODIFY OUTSIDE THIS COMMENT BLOCK */
                /********************************************/

                // Calculate directional lighting with light color
                // Note: _LightColor0 is already defined in UnityCG.cginc
                float3 lightColor = _LightColor0.rgb;
                float3 directional = NdotL * lightColor;

                // Calculate specular highlights using Blinn-Phong
                float3 halfVector = normalize(i.lightDir + i.viewDir);
                float NdotH = saturate(dot(normalize(i.normal), halfVector));
                float specularIntensity = pow(NdotH, _Glossiness * _Glossiness);
                float3 specular = specularIntensity * _SpecularColor.rgb;

                // Combine all components
                float3 finalColor = ambient + (toonColor * directional) + specular + rimColor;

                // Optionally, you can clamp the color to [0,1]
                finalColor = saturate(finalColor);

                return fixed4(finalColor, texColor.a);
            }
            ENDCG
        }

        // Shadow casting support.
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    FallBack "Diffuse"
}
