Shader "WorldEditor/GenshinStyleSky"
{
    Properties
    {
        [Header(Sun and Moon)]
        _SunColor ("Sun Color", Color) = (1, 1, 0.9, 1)
        _SunIntensity ("Sun Intensity", Range(0, 10)) = 5
        _SunSize ("Sun Size", Range(0, 1)) = 0.05
        _MoonColor ("Moon Color", Color) = (0.8, 0.8, 1, 1)
        _MoonIntensity ("Moon Intensity", Range(0, 2)) = 0.3
        _MoonSize ("Moon Size", Range(0, 1)) = 0.03
        
        [Header(Atmosphere)]
        _AtmosphereThickness ("Atmosphere Thickness", Range(0, 5)) = 1.5
        _RayleighScattering ("Rayleigh Scattering", Range(0, 10)) = 2.5
        _MieScattering ("Mie Scattering", Range(0, 10)) = 3
        _MieDirectionalG ("Mie Directional G", Range(0, 0.999)) = 0.758
        _SkyTint ("Sky Tint", Color) = (0.5, 0.5, 0.5, 1)
        _GroundColor ("Ground Color", Color) = (0.369, 0.349, 0.341, 1)
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        
        [Header(Clouds)]
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.4
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.6
        _CloudSpeed ("Cloud Speed", Range(0, 5)) = 1
        _CloudHeight ("Cloud Height", Range(1000, 10000)) = 3000
        _CloudThickness ("Cloud Thickness", Range(100, 2000)) = 500
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
        
        [Header(Weather Effects)]
        _WeatherIntensity ("Weather Intensity", Range(0, 1)) = 0
        _StormCloudColor ("Storm Cloud Color", Color) = (0.2, 0.2, 0.3, 1)
        _RainEffect ("Rain Effect", Range(0, 1)) = 0
        
        [Header(Stars)]
        _StarIntensity ("Star Intensity", Range(0, 2)) = 1
        _StarDensity ("Star Density", Range(0, 1)) = 0.5
        _StarTwinkle ("Star Twinkle", Range(0, 1)) = 0.5
        
        [Header(Horizon)]
        _HorizonFade ("Horizon Fade", Range(0, 1)) = 0.3
        _HorizonColor ("Horizon Color", Color) = (1, 0.8, 0.6, 1)
        
        [Header(Time of Day)]
        _TimeOfDay ("Time of Day", Range(0, 1)) = 0.5
        _SunDirection ("Sun Direction", Vector) = (0, 0.4, 0.8, 0)
        _MoonDirection ("Moon Direction", Vector) = (0, -0.4, -0.8, 0)
    }
    
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ STARS_ON
            #pragma multi_compile_local __ CLOUDS_ON
            #pragma multi_compile_local __ WEATHER_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _SunColor;
                float _SunIntensity;
                float _SunSize;
                float4 _MoonColor;
                float _MoonIntensity;
                float _MoonSize;
                
                float _AtmosphereThickness;
                float _RayleighScattering;
                float _MieScattering;
                float _MieDirectionalG;
                float4 _SkyTint;
                float4 _GroundColor;
                float _Exposure;
                
                float _CloudCoverage;
                float _CloudDensity;
                float _CloudSpeed;
                float _CloudHeight;
                float _CloudThickness;
                float4 _CloudColor;
                float4 _CloudShadowColor;
                
                float _WeatherIntensity;
                float4 _StormCloudColor;
                float _RainEffect;
                
                float _StarIntensity;
                float _StarDensity;
                float _StarTwinkle;
                
                float _HorizonFade;
                float4 _HorizonColor;
                
                float _TimeOfDay;
                float3 _SunDirection;
                float3 _MoonDirection;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDir = normalize(output.worldPos - _WorldSpaceCameraPos);
                
                return output;
            }
            
            // Atmospheric Scattering Functions
            float3 BetaR = float3(5.8e-3, 1.35e-2, 3.31e-2); // Rayleigh scattering coefficients
            float3 BetaM = float3(2e-2, 2e-2, 2e-2); // Mie scattering coefficients
            
            float RayleighPhase(float cosTheta)
            {
                return 3.0 / (16.0 * PI) * (1.0 + cosTheta * cosTheta);
            }
            
            float MiePhase(float cosTheta, float g)
            {
                float g2 = g * g;
                return 3.0 / (8.0 * PI) * (1.0 - g2) * (1.0 + cosTheta * cosTheta) / 
                       ((2.0 + g2) * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }
            
            // Noise functions for clouds and stars
            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float Noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(lerp(Hash(i + float3(0, 0, 0)), Hash(i + float3(1, 0, 0)), f.x),
                               lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                           lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                               lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }
            
            float FBM(float3 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * Noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }
            
            // Cloud rendering
            float GetCloudDensity(float3 worldPos)
            {
                float3 cloudPos = worldPos + float3(_Time.y * _CloudSpeed, 0, _Time.y * _CloudSpeed * 0.5);
                
                float noise = FBM(cloudPos * 0.0001, 4);
                float coverage = smoothstep(_CloudCoverage - 0.1, _CloudCoverage + 0.1, noise);
                
                float density = FBM(cloudPos * 0.0002, 6) * _CloudDensity;
                
                return coverage * density;
            }
            
            float4 RenderClouds(float3 viewDir, float3 sunDir)
            {
                if (viewDir.y < 0) return float4(0, 0, 0, 0);
                
                float t = (_CloudHeight - _WorldSpaceCameraPos.y) / max(viewDir.y, 0.01);
                float3 cloudPos = _WorldSpaceCameraPos + viewDir * t;
                
                float density = GetCloudDensity(cloudPos);
                
                if (density < 0.01) return float4(0, 0, 0, 0);
                
                // Simple lighting
                float3 lightDir = normalize(sunDir);
                float lightDot = dot(normalize(viewDir), lightDir);
                float scattering = lerp(0.2, 1.0, saturate(lightDot * 0.5 + 0.5));
                
                float3 cloudColor = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, scattering);
                
                // Weather effects
                cloudColor = lerp(cloudColor, _StormCloudColor.rgb, _WeatherIntensity);
                
                return float4(cloudColor, density);
            }
            
            // Star rendering
            float3 RenderStars(float3 viewDir)
            {
                if (viewDir.y < 0 || _TimeOfDay > 0.3 && _TimeOfDay < 0.7) return float3(0, 0, 0);
                
                float3 starPos = viewDir * 1000;
                float star = 0;
                
                for (int i = 0; i < 3; i++)
                {
                    float3 cell = floor(starPos);
                    float3 fract = frac(starPos);
                    
                    float hash = Hash(cell);
                    if (hash > 1.0 - _StarDensity)
                    {
                        float2 center = float2(Hash(cell + 1), Hash(cell + 2));
                        float dist = length(fract.xy - center);
                        float twinkle = sin(_Time.y * 10 + hash * 100) * _StarTwinkle + 1;
                        star += max(0, (0.05 - dist) * 20) * hash * twinkle;
                    }
                    
                    starPos *= 2.7;
                }
                
                float nightIntensity = 1.0 - saturate(abs(_TimeOfDay - 0.5) * 4 - 1);
                return star * _StarIntensity * nightIntensity * float3(1, 1, 1);
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 viewDir = normalize(input.viewDir);
                float3 sunDir = normalize(_SunDirection);
                float3 moonDir = normalize(_MoonDirection);
                
                // 太阳高度决定是白天还是夜晚
                float sunHeight = sunDir.y;
                bool isDaytime = sunHeight > -0.1; // 太阳稍微在地平线下也算白天（模拟晨昏）
                bool isSunVisible = sunHeight > 0; // 太阳完全在地平线上才可见
                
                // 定义不同时段的天空颜色
                float3 dayColor = float3(0.4, 0.7, 1.0);      // 白天蓝色
                float3 sunsetColor = float3(1.0, 0.5, 0.2);   // 日落橙色
                float3 nightColor = float3(0.02, 0.03, 0.08); // 夜晚深蓝黑
                
                float3 skyColor;
                
                if (isDaytime)
                {
                    if (sunHeight > 0.3) // 高太阳 - 白天
                    {
                        skyColor = dayColor;
                    }
                    else if (sunHeight > 0) // 低太阳 - 日出日落
                    {
                        float sunsetFactor = sunHeight / 0.3;
                        skyColor = lerp(sunsetColor, dayColor, sunsetFactor);
                    }
                    else // 太阳刚在地平线下 - 晨昏
                    {
                        float twilightFactor = (sunHeight + 0.1) / 0.1;
                        skyColor = lerp(nightColor, sunsetColor, twilightFactor);
                    }
                    
                    // 日出日落时地平线更亮
                    if (sunHeight < 0.3)
                    {
                        float horizon = 1.0 - abs(viewDir.y);
                        horizon = pow(horizon, 2.0);
                        skyColor = lerp(skyColor, sunsetColor, horizon * 0.5);
                    }
                }
                else // 夜晚
                {
                    skyColor = nightColor;
                }
                
                // 只在白天渲染太阳
                if (isSunVisible)
                {
                    float sunDistance = length(viewDir - sunDir);
                    float sunDisc = 1.0 - smoothstep(_SunSize * 0.5, _SunSize, sunDistance);
                    float3 sunColor = _SunColor.rgb * _SunIntensity;
                    
                    if (sunDisc > 0)
                    {
                        skyColor = lerp(skyColor, sunColor, sunDisc);
                    }
                    
                    // 太阳光晕
                    float sunGlow = 1.0 - smoothstep(_SunSize, _SunSize * 3.0, sunDistance);
                    skyColor += sunGlow * sunColor * 0.1;
                }
                
                // 只在夜晚渲染月亮
                if (!isDaytime && moonDir.y > 0)
                {
                    float moonDistance = length(viewDir - moonDir);
                    float moonDisc = 1.0 - smoothstep(_MoonSize * 0.5, _MoonSize, moonDistance);
                    
                    if (moonDisc > 0)
                    {
                        float3 moonColor = _MoonColor.rgb * _MoonIntensity;
                        skyColor = lerp(skyColor, moonColor, moonDisc);
                    }
                }
                
                // 地面处理
                if (viewDir.y < 0)
                {
                    skyColor = lerp(skyColor, _GroundColor.rgb, abs(viewDir.y));
                }
                
                // 星星（只在夜晚显示）
                #ifdef STARS_ON
                if (!isDaytime)
                {
                    skyColor += RenderStars(viewDir);
                }
                #endif
                
                // 云层
                #ifdef CLOUDS_ON
                float4 clouds = RenderClouds(viewDir, sunDir);
                skyColor = lerp(skyColor, clouds.rgb, clouds.a);
                #endif
                
                // 曝光控制
                skyColor *= _Exposure;
                
                return float4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Skybox/Procedural"
}