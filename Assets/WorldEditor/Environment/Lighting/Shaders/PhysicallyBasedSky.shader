Shader "WorldEditor/PhysicallyBasedSky"
{
    Properties
    {
        [Header(Atmospheric Parameters)]
        _PlanetRadius ("Planet Radius", Float) = 6371000
        _AtmosphereHeight ("Atmosphere Height", Float) = 80000
        _RayleighScaleHeight ("Rayleigh Scale Height", Float) = 8000
        _MieScaleHeight ("Mie Scale Height", Float) = 1200
        
        [Header(Scattering Coefficients)]
        _RayleighScattering ("Rayleigh Scattering", Vector) = (0.0000058, 0.0000135, 0.0000331, 0)
        _MieScattering ("Mie Scattering", Float) = 0.00002
        _MieAbsorption ("Mie Absorption", Float) = 0.0000044
        _MieG ("Mie G", Range(-1, 1)) = 0.8
        
        [Header(Ozone Layer)]
        _OzoneAbsorption ("Ozone Absorption", Vector) = (0.00000065, 0.000001881, 0.000000085, 0)
        _OzoneHeight ("Ozone Layer Height", Float) = 25000
        _OzoneThickness ("Ozone Layer Thickness", Float) = 15000
        
        [Header(Sun and Moon)]
        _SunIntensity ("Sun Intensity", Float) = 20
        _SunColor ("Sun Color", Color) = (1, 1, 1, 1)
        _SunSize ("Sun Size", Range(0, 0.1)) = 0.0045
        _MoonIntensity ("Moon Intensity", Float) = 0.4  
        _MoonColor ("Moon Color", Color) = (1, 1, 1, 1)
        _MoonSize ("Moon Size", Range(0, 0.1)) = 0.0087
        
        [Header(Night Sky)]
        _StarIntensity ("Star Intensity", Range(0, 2)) = 1
        _MilkyWayIntensity ("Milky Way Intensity", Range(0, 2)) = 1
        
        [Header(Ocean and Horizon)]
        _OceanColor ("Ocean Color", Color) = (0.1, 0.3, 0.6, 1)
        _HorizonSmoothness ("Horizon Smoothness", Range(0.01, 0.5)) = 0.15
        _WaterReflection ("Water Reflection", Range(0, 1)) = 0.3
        
        [Header(Exposure)]
        _Exposure ("Exposure", Range(0, 10)) = 1
        
        [Header(Directions)]
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _MoonDirection ("Moon Direction", Vector) = (0, -1, 0, 0)
    }
    
    SubShader
    {
        Tags { 
            "Queue"="Background" 
            "RenderType"="Background" 
            "PreviewType"="Skybox"
            "IgnoreProjector"="True"
        }
        Cull Off 
        ZWrite Off 
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ ATMOSPHERE_REFERENCE
            #pragma multi_compile_local __ STARS_ENABLED
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float _PlanetRadius;
                float _AtmosphereHeight;
                float _RayleighScaleHeight;
                float _MieScaleHeight;
                
                float4 _RayleighScattering;
                float _MieScattering;
                float _MieAbsorption;
                float _MieG;
                
                float4 _OzoneAbsorption;
                float _OzoneHeight;
                float _OzoneThickness;
                
                float _SunIntensity;
                float4 _SunColor;
                float _SunSize;
                float _MoonIntensity;
                float4 _MoonColor;
                float _MoonSize;
                
                float _StarIntensity;
                float _MilkyWayIntensity;
                
                float4 _OceanColor;
                float _HorizonSmoothness;
                float _WaterReflection;
                
                float _Exposure;
                
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
            
            // ========== 基于物理的大气散射实现 ==========
            // 基于 Bruneton 2008 论文的实现
            
            // 相位函数
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
            
            // 大气层交点计算
            float2 RaySphereIntersection(float3 rayOrigin, float3 rayDir, float radius)
            {
                float b = dot(rayOrigin, rayDir);
                float c = dot(rayOrigin, rayOrigin) - radius * radius;
                float discriminant = b * b - c;
                
                if (discriminant < 0.0)
                    return float2(-1, -1);
                
                float sqrtDiscriminant = sqrt(discriminant);
                return float2(-b - sqrtDiscriminant, -b + sqrtDiscriminant);
            }
            
            // 大气密度计算
            float GetDensity(float height, float scaleHeight)
            {
                return exp(-height / scaleHeight);
            }
            
            // 臭氧密度计算
            float GetOzoneDensity(float height)
            {
                float x = (height - _OzoneHeight) / _OzoneThickness;
                return max(0, 1.0 - abs(x));
            }
            
            // 光学深度计算（简化版本）
            float3 CalculateOpticalDepth(float3 rayOrigin, float3 rayDir, float rayLength, int steps)
            {
                float stepSize = rayLength / float(steps);
                float3 opticalDepth = float3(0, 0, 0);
                
                for (int i = 0; i < steps; i++)
                {
                    float3 pos = rayOrigin + rayDir * (float(i) + 0.5) * stepSize;
                    float height = length(pos) - _PlanetRadius;
                    
                    float rayleighDensity = GetDensity(height, _RayleighScaleHeight);
                    float mieDensity = GetDensity(height, _MieScaleHeight);
                    float ozoneDensity = GetOzoneDensity(height);
                    
                    opticalDepth.x += rayleighDensity * stepSize;
                    opticalDepth.y += mieDensity * stepSize;
                    opticalDepth.z += ozoneDensity * stepSize;
                }
                
                return opticalDepth;
            }
            
            // 大气散射计算主函数
            float3 CalculateAtmosphericScattering(float3 rayOrigin, float3 rayDir, float3 sunDir, float rayLength)
            {
                const int PRIMARY_STEPS = 16;
                const int LIGHT_STEPS = 8;
                
                float stepSize = rayLength / float(PRIMARY_STEPS);
                
                float3 totalRayleigh = float3(0, 0, 0);
                float3 totalMie = float3(0, 0, 0);
                
                float3 opticalDepthPA = float3(0, 0, 0);
                
                for (int i = 0; i < PRIMARY_STEPS; i++)
                {
                    float3 pos = rayOrigin + rayDir * (float(i) + 0.5) * stepSize;
                    float height = length(pos) - _PlanetRadius;
                    
                    // 计算当前点的密度
                    float rayleighDensity = GetDensity(height, _RayleighScaleHeight);
                    float mieDensity = GetDensity(height, _MieScaleHeight);
                    float ozoneDensity = GetOzoneDensity(height);
                    
                    opticalDepthPA.x += rayleighDensity * stepSize;
                    opticalDepthPA.y += mieDensity * stepSize;
                    opticalDepthPA.z += ozoneDensity * stepSize;
                    
                    // 计算到太阳的光学深度
                    float2 sunIntersection = RaySphereIntersection(pos, sunDir, _PlanetRadius + _AtmosphereHeight);
                    if (sunIntersection.y > 0)
                    {
                        float3 opticalDepthPB = CalculateOpticalDepth(pos, sunDir, sunIntersection.y, LIGHT_STEPS);
                        
                        float3 totalOpticalDepth = opticalDepthPA + opticalDepthPB;
                        
                        // 计算透射率
                        float3 rayleighTransmittance = exp(-_RayleighScattering.rgb * totalOpticalDepth.x);
                        float3 mieTransmittance = exp(-_MieScattering * totalOpticalDepth.y);
                        float3 ozoneTransmittance = exp(-_OzoneAbsorption.rgb * totalOpticalDepth.z);
                        
                        float3 transmittance = rayleighTransmittance * mieTransmittance * ozoneTransmittance;
                        
                        totalRayleigh += rayleighDensity * transmittance;
                        totalMie += mieDensity * transmittance;
                    }
                }
                
                float cosTheta = dot(rayDir, sunDir);
                float3 scattering = 
                    totalRayleigh * _RayleighScattering.rgb * RayleighPhase(cosTheta) +
                    totalMie * _MieScattering * MiePhase(cosTheta, _MieG);
                
                return scattering * stepSize * _SunIntensity;
            }
            
            // 星空渲染
            float Hash(float2 p) 
            {
                float3 p3 = frac(float3(p.xyx) * 0.13);
                p3 += dot(p3, p3.yzx + 3.333);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float3 RenderStars(float3 viewDir)
            {
                if (viewDir.y < 0) return float3(0, 0, 0); // 朝下时不渲染星星
                
                float2 starCoord = viewDir.xz / (viewDir.y + 0.1) * 100;
                float2 starGrid = floor(starCoord);
                float2 starFract = frac(starCoord);
                
                float star = 0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cell = starGrid + float2(x, y);
                        float hash = Hash(cell);
                        
                        if (hash > 0.95)
                        {
                            float2 center = float2(Hash(cell + 1), Hash(cell + 2));
                            float2 pos = starFract - float2(x, y) - center;
                            float dist = length(pos);
                            
                            float brightness = pow(hash, 2.0);
                            star += max(0, (0.05 - dist) * 20) * brightness;
                        }
                    }
                }
                
                return star * _StarIntensity * float3(1, 1, 1);
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 viewDir = normalize(input.viewDir);
                float3 sunDir = normalize(_SunDirection);
                float3 moonDir = normalize(_MoonDirection);
                
                // 改进地平线处理 - 创建平滑的海天过渡而非硬边界
                float horizonFade = 1.0;
                if (viewDir.y < 0.0)
                {
                    horizonFade = saturate(1.0 + viewDir.y * 10.0); // 在地平线以下平滑衰减
                }
                
                // 计算射线起点（地表）
                float3 rayOrigin = float3(0, _PlanetRadius, 0);
                
                // 计算与大气层的交点
                float2 atmosphereIntersection = RaySphereIntersection(rayOrigin, viewDir, _PlanetRadius + _AtmosphereHeight);
                
                float3 skyColor = float3(0, 0, 0);
                
                // 只有射线与大气层相交时才计算散射
                if (atmosphereIntersection.y > 0)
                {
                    float rayLength = atmosphereIntersection.y;
                    
                    // 如果射线与地面相交，限制长度
                    float2 groundIntersection = RaySphereIntersection(rayOrigin, viewDir, _PlanetRadius);
                    if (groundIntersection.x > 0)
                        rayLength = min(rayLength, groundIntersection.x);
                    
                    // 计算大气散射
                    skyColor = CalculateAtmosphericScattering(rayOrigin, viewDir, sunDir, rayLength);
                }
                
                // 渲染太阳
                if (_SunDirection.y > -0.1)
                {
                    float sunDistance = length(viewDir - sunDir);
                    float sunDisc = 1.0 - smoothstep(_SunSize * 0.5, _SunSize, sunDistance);
                    
                    if (sunDisc > 0)
                    {
                        float3 sunColor = _SunColor.rgb * _SunIntensity * 0.1;
                        skyColor = max(skyColor, sunColor * sunDisc);
                    }
                }
                
                // 渲染月亮
                if (_MoonDirection.y > 0 && _SunDirection.y < -0.1)
                {
                    float moonDistance = length(viewDir - moonDir);
                    float moonDisc = 1.0 - smoothstep(_MoonSize * 0.5, _MoonSize, moonDistance);
                    
                    if (moonDisc > 0)
                    {
                        float3 moonColor = _MoonColor.rgb * _MoonIntensity;
                        skyColor += moonColor * moonDisc;
                    }
                }
                
                // 渲染星空
                #ifdef STARS_ENABLED
                if (_SunDirection.y < -0.05)
                {
                    float starVisibility = saturate((-_SunDirection.y - 0.05) / 0.1);
                    skyColor += RenderStars(viewDir) * starVisibility;
                }
                #endif
                
                // 地平线和海洋处理
                if (viewDir.y < 0.1) // 接近地平线或朝下
                {
                    // 基础海洋颜色
                    float3 waterColor = _OceanColor.rgb;
                    
                    // 根据太阳位置调整海洋颜色
                    float sunBrightness = saturate(_SunDirection.y + 0.1);
                    
                    if (sunBrightness > 0.5) // 白天 - 使用设定的海洋颜色
                    {
                        // 保持基础海洋颜色
                    }
                    else if (sunBrightness > 0.2) // 黄昏 - 添加金色反射
                    {
                        float3 sunsetReflection = float3(1.0, 0.6, 0.2);
                        waterColor = lerp(waterColor, sunsetReflection, _WaterReflection);
                    }
                    else // 夜晚 - 变暗
                    {
                        waterColor *= 0.3; // 夜晚水面较暗
                    }
                    
                    // 添加太阳在水面的反射
                    if (_SunDirection.y > 0)
                    {
                        float3 reflectedSunDir = float3(_SunDirection.x, -_SunDirection.y, _SunDirection.z);
                        float sunReflection = pow(saturate(dot(viewDir, reflectedSunDir)), 10.0);
                        waterColor += _SunColor.rgb * sunReflection * _WaterReflection;
                    }
                    
                    // 可调节的平滑地平线过渡
                    float horizonBlend = saturate((viewDir.y + 0.1) / _HorizonSmoothness);
                    skyColor = lerp(waterColor, skyColor, horizonBlend);
                }
                
                // 应用地平线衰减
                skyColor *= horizonFade;
                
                // 曝光
                skyColor *= _Exposure;
                
                // 确保海天交界处的alpha值正确
                float alpha = viewDir.y < -0.05 ? horizonFade : 1.0;
                
                return float4(skyColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Skybox/Procedural"
}