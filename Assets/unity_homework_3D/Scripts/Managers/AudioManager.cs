using UnityEngine;
using UnityEngine.Audio;

namespace Managers
{
    /// <summary>
    /// Audio management system for game sounds
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        
        [Header("Background Music")]
        [SerializeField] private AudioClip backgroundMusic;
        
        [Header("Weapon Sounds")]
        [SerializeField] private AudioClip singleFireSound;
        [SerializeField] private AudioClip autoFireSound;
        [SerializeField] private AudioClip weaponSwitchSound;
        
        [Header("Effect Sounds")]
        [SerializeField] private AudioClip impactSound;
        
        private AudioSource _musicAudioSource;
        private AudioSource _sfxAudioSource;
        
        // Audio state
        private bool _masterEnabled = true;
        private float _musicVolume = 0.75f;
        private float _sfxVolume = 0.75f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAudio()
        {
            SetupAudioSources();
            AssignMixerGroups();
            StartBackgroundMusic();
            SubscribeToGameEvents();
        }
        
        private void SetupAudioSources()
        {
            if (!_musicAudioSource)
            {
                _musicAudioSource = gameObject.AddComponent<AudioSource>();
                _musicAudioSource.playOnAwake = false;
                _musicAudioSource.loop = true;
                _musicAudioSource.spatialBlend = 0f;
            }
            
            if (!_sfxAudioSource)
            {
                _sfxAudioSource = gameObject.AddComponent<AudioSource>();
                _sfxAudioSource.playOnAwake = false;
                _sfxAudioSource.spatialBlend = 0f;
            }
        }
        
        private void AssignMixerGroups()
        {
            if (!audioMixer) return;
            
            var musicGroups = audioMixer.FindMatchingGroups("Music");
            var sfxGroups = audioMixer.FindMatchingGroups("SFX");
            
            if (musicGroups.Length > 0)
                _musicAudioSource.outputAudioMixerGroup = musicGroups[0];
            
            if (sfxGroups.Length > 0)
                _sfxAudioSource.outputAudioMixerGroup = sfxGroups[0];
        }
        
        private void StartBackgroundMusic()
        {
            if (backgroundMusic && _musicAudioSource)
            {
                _musicAudioSource.clip = backgroundMusic;
                _musicAudioSource.Play();
            }
        }
        
        // Settings methods
        public void SetMasterEnabled(bool enabled)
        {
            _masterEnabled = enabled;
            ApplyAudioSettings();
        }
        
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
        }
        
        private void ApplyAudioSettings()
        {
            if (!audioMixer) return;
            
            if (_masterEnabled)
            {
                float musicDB = _musicVolume > 0 ? Mathf.Log10(_musicVolume) * 20 : -80f;
                float sfxDB = _sfxVolume > 0 ? Mathf.Log10(_sfxVolume) * 20 : -80f;
                
                audioMixer.SetFloat("MusicVolume", musicDB);
                audioMixer.SetFloat("SFXVolume", sfxDB);
            }
            else
            {
                audioMixer.SetFloat("MusicVolume", -80f);
                audioMixer.SetFloat("SFXVolume", -80f);
            }
        }
        
        public void PlayWeaponFire(bool isFullAuto = false)
        {
            AudioClip clipToPlay = isFullAuto ? autoFireSound : singleFireSound;
            if (clipToPlay && _sfxAudioSource)
                _sfxAudioSource.PlayOneShot(clipToPlay);
        }
        
        public void PlayWeaponSwitch()
        {
            if (weaponSwitchSound && _sfxAudioSource)
                _sfxAudioSource.PlayOneShot(weaponSwitchSound);
        }
        
        public void PlayImpact()
        {
            if (impactSound && _sfxAudioSource)
                _sfxAudioSource.PlayOneShot(impactSound);
        }
        
        // 3D positional audio for enemies
        public void PlayWeaponFireAt(Vector3 position, bool isFullAuto = false)
        {
            AudioClip clipToPlay = isFullAuto ? autoFireSound : singleFireSound;
            if (clipToPlay)
                AudioSource.PlayClipAtPoint(clipToPlay, position);
        }
        
        public void PlayImpactAt(Vector3 position)
        {
            if (impactSound)
                AudioSource.PlayClipAtPoint(impactSound, position);
        }
        
        private void SubscribeToGameEvents()
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.OnGameRestarted += OnGameRestarted;
            }
        }

        private void OnGameRestarted()
        {
            StartBackgroundMusic();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.OnGameRestarted -= OnGameRestarted;
            }
        }
        
        // Getters
        public bool IsMasterEnabled() => _masterEnabled;
        public float GetMusicVolume() => _musicVolume;
        public float GetSFXVolume() => _sfxVolume;
    }
}