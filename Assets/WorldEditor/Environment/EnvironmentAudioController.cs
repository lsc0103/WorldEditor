using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldEditor.Core;

namespace WorldEditor.Environment
{
    /// <summary>
    /// 环境音频控制器 - 管理环境音效和音频氛围
    /// </summary>
    public class EnvironmentAudioController : MonoBehaviour
    {
        [Header("音频源")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioSource weatherAudioSource;
        [SerializeField] private AudioSource windAudioSource;
        [SerializeField] private AudioSource[] randomAudioSources = new AudioSource[4];
        
        [Header("环境音效")]
        [SerializeField] private AudioClip[] forestAmbient;
        [SerializeField] private AudioClip[] desertAmbient;
        [SerializeField] private AudioClip[] oceanAmbient;
        [SerializeField] private AudioClip[] mountainAmbient;
        [SerializeField] private AudioClip[] cityAmbient;
        [SerializeField] private AudioClip[] countrysideAmbient;
        
        [Header("天气音效")]
        [SerializeField] private AudioClip[] rainSounds;
        [SerializeField] private AudioClip[] windSounds;
        [SerializeField] private AudioClip[] thunderSounds;
        [SerializeField] private AudioClip[] snowSounds;
        
        [Header("随机音效")]
        [SerializeField] private AudioClip[] birdSounds;
        [SerializeField] private AudioClip[] insectSounds;
        [SerializeField] private AudioClip[] animalSounds;
        [SerializeField] private AudioClip[] waterSounds;
        
        [Header("音频设置")]
        [SerializeField] private bool enableEnvironmentAudio = true;
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float ambientVolume = 0.8f;
        [SerializeField] private float weatherVolume = 1f;
        [SerializeField] private float randomSoundVolume = 0.6f;
        
        [Header("3D音频")]
        [SerializeField] private bool enable3DAudio = true;
        [SerializeField] private float maxAudioDistance = 100f;
        [SerializeField] private float audioFadeSpeed = 2f;
        
        [Header("随机播放")]
        [SerializeField] private bool enableRandomSounds = true;
        [SerializeField] private Vector2 randomSoundInterval = new Vector2(5f, 15f);
        [SerializeField] private float randomSoundChance = 0.3f;
        
        // 私有变量
        private DynamicEnvironmentSystem environmentSystem;
        private AmbientSoundProfile currentProfile = AmbientSoundProfile.Forest;
        private Camera mainCamera;
        
        // 音频状态
        private Coroutine randomSoundCoroutine;
        private Dictionary<AmbientSoundProfile, AudioClip[]> ambientSoundMap;
        private float currentAmbientVolume;
        private float currentWeatherVolume;
        
        public void Initialize(DynamicEnvironmentSystem envSystem)
        {
            environmentSystem = envSystem;
            mainCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            
            SetupAudioSources();
            SetupAmbientSoundMap();
            StartRandomSounds();
        }
        
        void SetupAudioSources()
        {
            // 设置环境音频源
            if (ambientAudioSource == null)
            {
                ambientAudioSource = CreateAudioSource("Ambient Audio", false, true);
            }
            
            // 设置天气音频源
            if (weatherAudioSource == null)
            {
                weatherAudioSource = CreateAudioSource("Weather Audio", false, true);
            }
            
            // 设置风音频源
            if (windAudioSource == null)
            {
                windAudioSource = CreateAudioSource("Wind Audio", false, true);
            }
            
            // 设置随机音频源
            for (int i = 0; i < randomAudioSources.Length; i++)
            {
                if (randomAudioSources[i] == null)
                {
                    randomAudioSources[i] = CreateAudioSource($"Random Audio {i}", enable3DAudio, false);
                }
            }
        }
        
        AudioSource CreateAudioSource(string name, bool is3D, bool loop)
        {
            GameObject audioObj = new GameObject(name);
            audioObj.transform.SetParent(transform);
            
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = loop;
            audioSource.spatialBlend = is3D ? 1f : 0f; // 0=2D, 1=3D
            
            if (is3D)
            {
                audioSource.maxDistance = maxAudioDistance;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.dopplerLevel = 0f;
            }
            
            return audioSource;
        }
        
        void SetupAmbientSoundMap()
        {
            ambientSoundMap = new Dictionary<AmbientSoundProfile, AudioClip[]>
            {
                { AmbientSoundProfile.Forest, forestAmbient },
                { AmbientSoundProfile.Desert, desertAmbient },
                { AmbientSoundProfile.Ocean, oceanAmbient },
                { AmbientSoundProfile.Mountain, mountainAmbient },
                { AmbientSoundProfile.City, cityAmbient },
                { AmbientSoundProfile.Countryside, countrysideAmbient }
            };
        }
        
        void StartRandomSounds()
        {
            if (enableRandomSounds)
            {
                randomSoundCoroutine = StartCoroutine(RandomSoundCoroutine());
            }
        }
        
        IEnumerator RandomSoundCoroutine()
        {
            while (enableRandomSounds)
            {
                float waitTime = Random.Range(randomSoundInterval.x, randomSoundInterval.y);
                yield return new WaitForSeconds(waitTime);
                
                if (Random.value < randomSoundChance)
                {
                    PlayRandomSound();
                }
            }
        }
        
        void PlayRandomSound()
        {
            // 选择可用的音频源
            AudioSource availableSource = GetAvailableRandomAudioSource();
            if (availableSource == null) return;
            
            // 根据当前环境选择合适的随机音效
            AudioClip[] soundPool = GetRandomSoundPool();
            if (soundPool == null || soundPool.Length == 0) return;
            
            AudioClip selectedClip = soundPool[Random.Range(0, soundPool.Length)];
            if (selectedClip == null) return;
            
            // 设置3D位置（如果启用3D音频）
            if (enable3DAudio && mainCamera != null)
            {
                Vector3 randomPosition = GetRandomAudioPosition();
                availableSource.transform.position = randomPosition;
            }
            
            // 播放音效
            availableSource.clip = selectedClip;
            availableSource.volume = randomSoundVolume * masterVolume;
            availableSource.Play();
        }
        
        AudioSource GetAvailableRandomAudioSource()
        {
            foreach (var source in randomAudioSources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }
            return null;
        }
        
        AudioClip[] GetRandomSoundPool()
        {
            switch (currentProfile)
            {
                case AmbientSoundProfile.Forest:
                    return Random.value > 0.5f ? birdSounds : insectSounds;
                case AmbientSoundProfile.Desert:
                    return windSounds;
                case AmbientSoundProfile.Ocean:
                    return waterSounds;
                case AmbientSoundProfile.Mountain:
                    return Random.value > 0.7f ? birdSounds : windSounds;
                case AmbientSoundProfile.City:
                    return null; // 城市音效需要特殊处理
                case AmbientSoundProfile.Countryside:
                    return Random.value > 0.6f ? birdSounds : animalSounds;
                default:
                    return birdSounds;
            }
        }
        
        Vector3 GetRandomAudioPosition()
        {
            if (mainCamera == null) return Vector3.zero;
            
            // 在摄像机周围生成随机位置
            Vector3 cameraPos = mainCamera.transform.position;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(10f, maxAudioDistance * 0.8f);
            
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                Random.Range(-5f, 20f), // 高度变化
                Mathf.Sin(angle) * distance
            );
            
            return cameraPos + offset;
        }
        
        public void UpdateAudio(float deltaTime, EnvironmentState environmentState)
        {
            if (!enableEnvironmentAudio) return;
            
            // 更新环境音效
            UpdateAmbientAudio(deltaTime, environmentState);
            
            // 更新天气音效
            UpdateWeatherAudio(deltaTime, environmentState);
            
            // 更新风音效
            UpdateWindAudio(deltaTime, environmentState);
            
            // 更新音量
            UpdateAudioVolumes(deltaTime);
        }
        
        void UpdateAmbientAudio(float deltaTime, EnvironmentState environmentState)
        {
            if (ambientAudioSource == null) return;
            
            // 获取当前环境音效
            AudioClip[] currentAmbientSounds = GetCurrentAmbientSounds();
            if (currentAmbientSounds == null || currentAmbientSounds.Length == 0) return;
            
            // 如果需要切换音效
            AudioClip targetClip = currentAmbientSounds[Random.Range(0, currentAmbientSounds.Length)];
            
            if (ambientAudioSource.clip != targetClip)
            {
                StartCoroutine(CrossfadeAmbientAudio(targetClip));
            }
        }
        
        IEnumerator CrossfadeAmbientAudio(AudioClip newClip)
        {
            if (newClip == null) yield break;
            
            float fadeTime = 2f;
            float originalVolume = ambientAudioSource.volume;
            
            // 淡出当前音效
            float timer = 0f;
            while (timer < fadeTime && ambientAudioSource.isPlaying)
            {
                timer += Time.deltaTime;
                ambientAudioSource.volume = Mathf.Lerp(originalVolume, 0f, timer / fadeTime);
                yield return null;
            }
            
            // 切换音效
            ambientAudioSource.clip = newClip;
            ambientAudioSource.Play();
            
            // 淡入新音效
            timer = 0f;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                ambientAudioSource.volume = Mathf.Lerp(0f, originalVolume, timer / fadeTime);
                yield return null;
            }
        }
        
        AudioClip[] GetCurrentAmbientSounds()
        {
            if (ambientSoundMap.ContainsKey(currentProfile))
            {
                return ambientSoundMap[currentProfile];
            }
            return forestAmbient; // 默认返回森林音效
        }
        
        void UpdateWeatherAudio(float deltaTime, EnvironmentState environmentState)
        {
            if (weatherAudioSource == null) return;
            
            AudioClip weatherClip = GetWeatherAudioClip(environmentState.currentWeather);
            
            if (weatherClip != weatherAudioSource.clip)
            {
                if (weatherClip != null)
                {
                    weatherAudioSource.clip = weatherClip;
                    weatherAudioSource.Play();
                }
                else
                {
                    weatherAudioSource.Stop();
                }
            }
            
            // 调整天气音效强度
            float targetVolume = CalculateWeatherVolume(environmentState);
            currentWeatherVolume = Mathf.Lerp(currentWeatherVolume, targetVolume, deltaTime * audioFadeSpeed);
        }
        
        AudioClip GetWeatherAudioClip(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rainy:
                case WeatherType.Stormy:
                    return rainSounds != null && rainSounds.Length > 0 ? rainSounds[0] : null;
                case WeatherType.Snowy:
                    return snowSounds != null && snowSounds.Length > 0 ? snowSounds[0] : null;
                default:
                    return null;
            }
        }
        
        float CalculateWeatherVolume(EnvironmentState environmentState)
        {
            switch (environmentState.currentWeather)
            {
                case WeatherType.Rainy:
                    return environmentState.precipitationIntensity * weatherVolume;
                case WeatherType.Stormy:
                    return environmentState.precipitationIntensity * weatherVolume * 1.2f;
                case WeatherType.Snowy:
                    return environmentState.precipitationIntensity * weatherVolume * 0.6f;
                default:
                    return 0f;
            }
        }
        
        void UpdateWindAudio(float deltaTime, EnvironmentState environmentState)
        {
            if (windAudioSource == null) return;
            
            // 根据风力强度播放风声
            float windIntensity = environmentState.windStrength;
            
            if (windIntensity > 0.2f)
            {
                if (!windAudioSource.isPlaying && windSounds != null && windSounds.Length > 0)
                {
                    windAudioSource.clip = windSounds[0];
                    windAudioSource.Play();
                }
                
                float targetVolume = windIntensity * weatherVolume * 0.7f;
                windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, targetVolume, deltaTime * audioFadeSpeed);
            }
            else
            {
                if (windAudioSource.isPlaying)
                {
                    windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, 0f, deltaTime * audioFadeSpeed);
                    
                    if (windAudioSource.volume < 0.01f)
                    {
                        windAudioSource.Stop();
                    }
                }
            }
        }
        
        void UpdateAudioVolumes(float deltaTime)
        {
            // 更新主音量
            if (ambientAudioSource != null)
            {
                float targetAmbientVolume = ambientVolume * masterVolume;
                ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, targetAmbientVolume, deltaTime * audioFadeSpeed);
            }
            
            if (weatherAudioSource != null)
            {
                weatherAudioSource.volume = currentWeatherVolume * masterVolume;
            }
            
            if (windAudioSource != null)
            {
                windAudioSource.volume *= masterVolume;
            }
        }
        
        public void SetAmbientProfile(AmbientSoundProfile profile)
        {
            if (currentProfile != profile)
            {
                currentProfile = profile;
                
                // 立即切换到新的环境音效
                AudioClip[] newAmbientSounds = GetCurrentAmbientSounds();
                if (newAmbientSounds != null && newAmbientSounds.Length > 0 && ambientAudioSource != null)
                {
                    AudioClip newClip = newAmbientSounds[Random.Range(0, newAmbientSounds.Length)];
                    StartCoroutine(CrossfadeAmbientAudio(newClip));
                }
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }
        
        public void PlayThunderSound()
        {
            if (thunderSounds != null && thunderSounds.Length > 0 && weatherAudioSource != null)
            {
                AudioClip thunderClip = thunderSounds[Random.Range(0, thunderSounds.Length)];
                weatherAudioSource.PlayOneShot(thunderClip, weatherVolume * masterVolume);
            }
        }
        
        public void SetEnableRandomSounds(bool enable)
        {
            enableRandomSounds = enable;
            
            if (enable && randomSoundCoroutine == null)
            {
                randomSoundCoroutine = StartCoroutine(RandomSoundCoroutine());
            }
            else if (!enable && randomSoundCoroutine != null)
            {
                StopCoroutine(randomSoundCoroutine);
                randomSoundCoroutine = null;
            }
        }
        
        void OnDestroy()
        {
            if (randomSoundCoroutine != null)
            {
                StopCoroutine(randomSoundCoroutine);
            }
        }
    }
}