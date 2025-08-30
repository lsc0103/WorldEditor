Shader "WorldEditor/ProfessionalSky"
{
    Properties
    {
        [Header(Celestial Bodies)]
        _SunColor ("Sun Color", Color) = (1, 1, 0.9, 1)
        _SunIntensity ("Sun Intensity", Range(0, 20)) = 10
        _SunSize ("Sun Size", Range(0, 0.1)) = 0.04
        _MoonColor ("Moon Color", Color) = (0.8, 0.85, 1, 1)
        _MoonIntensity ("Moon Intensity", Range(0, 5)) = 1
        _MoonSize ("Moon Size", Range(0, 0.1)) = 0.03
        
        [Header(Atmospheric Scattering)]
        _AtmosphereThickness ("Atmosphere Thickness", Range(0, 5)) = 1.0
        _RayleighScattering ("Rayleigh Scattering", Range(0, 10)) = 5.5
        _MieScattering ("Mie Scattering", Range(0, 10)) = 1.3
        _MieDirectionalG ("Mie Directional G", Range(-0.999, 0.999)) = 0.758
        _ScatteringIntensity ("Scattering Intensity", Range(0, 5)) = 1.0
        
        [Header(Sky Colors)]
        _DayTopColor ("Day Top Color", Color) = (0.4, 0.7, 1.0, 1)
        _DayHorizonColor ("Day Horizon Color", Color) = (0.9, 0.9, 1.0, 1)
        _SunsetTopColor ("Sunset Top Color", Color) = (0.8, 0.5, 0.3, 1)
        _SunsetHorizonColor ("Sunset Horizon Color", Color) = (1.0, 0.6, 0.2, 1)
        _NightTopColor ("Night Top Color", Color) = (0.01, 0.01, 0.05, 1)
        _NightHorizonColor ("Night Horizon Color", Color) = (0.02, 0.02, 0.08, 1)
        
        [Header(Time and Position)]
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _MoonDirection ("Moon Direction", Vector) = (0, -1, 0, 0)
        
        [Header(Stars)]
        _StarIntensity ("Star Intensity", Range(0, 2)) = 1
        _StarDensity ("Star Density", Range(0, 1)) = 0.5
        _StarTwinkle ("Star Twinkle", Range(0, 1)) = 0.5
        
        [Header(Advanced)]
        _Exposure ("Exposure", Range(0, 5)) = 1.3
        _Gamma ("Gamma", Range(0.4, 3.0)) = 2.2
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
            #pragma multi_compile_local __ STARS_ENABLED
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
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
                float _ScatteringIntensity;
                
                float4 _DayTopColor;
                float4 _DayHorizonColor;
                float4 _SunsetTopColor;
                float4 _SunsetHorizonColor;
                float4 _NightTopColor;
                float4 _NightHorizonColor;
                
                float3 _SunDirection;
                float3 _MoonDirection;
                
                float _StarIntensity;
                float _StarDensity;
                float _StarTwinkle;
                
                float _Exposure;
                float _Gamma;
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
            
            // 专业级大气散射函数 - 基于Bruneton模型的简化实现
            
            // Rayleigh散射系数 (波长相关)
            static const float3 RayleighCoeff = float3(5.8e-3, 1.35e-2, 3.31e-2);
            
            // Mie散射系数
            static const float MieCoeff = 2e-2;
            
            // Rayleigh相位函数
            float RayleighPhase(float cosTheta)
            {
                return 3.0 / (16.0 * PI) * (1.0 + cosTheta * cosTheta);
            }
            
            // Mie相位函数（Henyey-Greenstein)
            float MiePhase(float cosTheta, float g)
            {
                float g2 = g * g;
                return 3.0 / (8.0 * PI) * (1.0 - g2) * (1.0 + cosTheta * cosTheta) / 
                       ((2.0 + g2) * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }
            
            // 计算大气散射
            float3 CalculateScattering(float3 viewDir, float3 sunDir, float sunIntensity)
            {
                float cosTheta = dot(viewDir, sunDir);
                float altitude = max(0.0, viewDir.y);
                
                // 大气密度随高度衰减
                float atmosphereDensity = exp(-altitude * _AtmosphereThickness);
                
                // Rayleigh散射
                float3 rayleigh = RayleighCoeff * _RayleighScattering * RayleighPhase(cosTheta);
                
                // Mie散射
                float3 mie = float3(MieCoeff, MieCoeff, MieCoeff) * _MieScattering * MiePhase(cosTheta, _MieDirectionalG);
                
                // 结合大气密度和太阳强度
                float3 scattering = (rayleigh + mie) * atmosphereDensity * sunIntensity * _ScatteringIntensity;
                
                return scattering;
            }
            
            // 星空噪声函数
            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            // 渲染星空
            float3 RenderStars(float3 viewDir, float nightFactor)
            {
                if (nightFactor < 0.1 || viewDir.y < 0) return float3(0, 0, 0);
                
                float3 starPos = viewDir * 1000;
                float star = 0;
                
                // 多层星空效果
                for (int i = 0; i < 3; i++)
                {
                    float3 cell = floor(starPos);
                    float3 fract = frac(starPos);
                    
                    float hash = Hash(cell);
                    if (hash > 1.0 - _StarDensity)
                    {
                        float2 center = float2(Hash(cell + 1), Hash(cell + 2));
                        float dist = length(fract.xy - center);
                        
                        // 星星闪烁效果
                        float twinkle = sin(_Time.y * 5 + hash * 50) * _StarTwinkle + (1.0 - _StarTwinkle);
                        star += max(0, (0.03 - dist) * 30) * hash * twinkle;
                    }
                    
                    starPos *= 2.3;
                }
                
                return star * _StarIntensity * nightFactor * float3(1, 1, 1);
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 viewDir = normalize(input.viewDir);
                float3 sunDir = normalize(_SunDirection);
                float3 moonDir = normalize(_MoonDirection);
                
                // 精确的太阳高度分析 - 实现真实的昼夜交替
                float sunElevation = sunDir.y;
                
                // 重新定义时间段 - 更严格的边界，确保真实的日出效果
                float nightFactor = saturate((-sunElevation - 0.05) / 0.15);        // 夜晚：太阳高度 < -0.05
                float twilightFactor = 1.0 - abs(sunElevation) / 0.15;              // 黎明/黄昏：太阳接近地平线
                twilightFactor = saturate(twilightFactor) * (1.0 - nightFactor) * (1.0 - saturate((sunElevation - 0.1) / 0.2));
                float dayFactor = saturate((sunElevation - 0.1) / 0.3);             // 白天：太阳高度 > 0.1
                
                float altitude = saturate(viewDir.y);
                
                // 定义真实的天空颜色 - 更戏剧性的对比
                float3 nightTop = float3(0.002, 0.005, 0.015);         // 夜晚天顶：深蓝黑
                float3 nightHorizon = float3(0.005, 0.010, 0.025);     // 夜晚地平线：稍亮蓝黑
                
                float3 twilightTop = float3(0.2, 0.1, 0.4);            // 黎明天顶：深紫
                float3 twilightHorizon = float3(1.0, 0.4, 0.1);        // 黎明地平线：金橙
                
                float3 dayTop = float3(0.4, 0.7, 1.0);                 // 白天天顶：天蓝
                float3 dayHorizon = float3(0.8, 0.9, 1.0);             // 白天地平线：淡蓝
                
                // 混合天空颜色 - 确保夜晚足够黑暗
                float3 baseTop = nightTop * nightFactor + twilightTop * twilightFactor + dayTop * dayFactor;
                float3 baseHorizon = nightHorizon * nightFactor + twilightHorizon * twilightFactor + dayHorizon * dayFactor;
                
                // 太阳方向的增强效果 - 只在黎明/黄昏时期
                float sunDirectionInfluence = 0;
                if (twilightFactor > 0 && sunElevation > -0.1)
                {
                    float sunDot = dot(normalize(viewDir), sunDir);
                    sunDirectionInfluence = pow(saturate(sunDot), 4.0) * twilightFactor * 0.5;
                    baseHorizon += float3(1.0, 0.6, 0.2) * sunDirectionInfluence;
                }
                
                float3 skyColor = lerp(baseHorizon, baseTop, altitude);
                
                // 添加大气散射（只有太阳接近或在地平线以上时）
                if (sunElevation > -0.05)
                {
                    float scatteringIntensity = saturate((sunElevation + 0.05) / 0.15);
                    skyColor += CalculateScattering(viewDir, sunDir, scatteringIntensity);
                }
                
                // 渲染太阳（只在地平线以上时）- 实现"阳光乍破"效果
                if (sunElevation > 0)
                {
                    float sunDistance = length(viewDir - sunDir);
                    float sunDisc = 1.0 - smoothstep(_SunSize * 0.5, _SunSize, sunDistance);
                    
                    if (sunDisc > 0)
                    {
                        // 太阳强度随高度急剧增加，模拟"乍现"效果
                        float sunVisibility = pow(saturate(sunElevation / 0.1), 2.0);
                        float3 sunColor = _SunColor.rgb * _SunIntensity * sunVisibility;
                        skyColor = max(skyColor, sunColor * sunDisc);
                        
                        // 增强的太阳光晕，在日出时更明显
                        float sunGlow = 1.0 - smoothstep(_SunSize, _SunSize * 6.0, sunDistance);
                        skyColor += sunGlow * sunColor * 0.2 * sunVisibility;
                    }
                }
                
                // 渲染月亮（只在深夜且月亮在地平线以上）
                if (nightFactor > 0.5 && moonDir.y > 0)
                {
                    float moonDistance = length(viewDir - moonDir);
                    float moonDisc = 1.0 - smoothstep(_MoonSize * 0.5, _MoonSize, moonDistance);
                    
                    if (moonDisc > 0)
                    {
                        float3 moonColor = _MoonColor.rgb * _MoonIntensity;
                        skyColor = max(skyColor, moonColor * moonDisc);
                    }
                }
                
                // 添加星空（只在夜晚）
                #ifdef STARS_ENABLED
                skyColor += RenderStars(viewDir, nightFactor);
                #endif
                
                // 地面颜色处理
                if (viewDir.y < 0)
                {
                    float3 groundColor = lerp(skyColor, float3(0.1, 0.1, 0.1), abs(viewDir.y));
                    skyColor = groundColor;
                }
                
                // 曝光和色调映射
                skyColor *= _Exposure;
                skyColor = pow(skyColor, 1.0 / _Gamma);
                
                // HDR色调映射
                skyColor = skyColor / (1.0 + skyColor);
                
                return float4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Skybox/Procedural"
}