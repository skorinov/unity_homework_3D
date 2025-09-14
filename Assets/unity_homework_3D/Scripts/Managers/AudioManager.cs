using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Centralized audio management system
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource weaponAudioSource;
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioSource ambientAudioSource;
        
        [Header("Weapon Sounds")]
        [SerializeField] private AudioClip[] fireSounds;
        [SerializeField] private AudioClip[] reloadSounds;
        [SerializeField] private AudioClip[] impactSounds;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // Find root object and make persistent
                Transform rootTransform = transform;
                while (rootTransform.parent != null)
                {
                    rootTransform = rootTransform.parent;
                }
                
                DontDestroyOnLoad(rootTransform.gameObject);
                
                InitializeAudioSources();
            }
            else
            {
                // Destroy duplicate
                Transform rootToDestroy = transform;
                while (rootToDestroy.parent != null)
                {
                    rootToDestroy = rootToDestroy.parent;
                }
                Destroy(rootToDestroy.gameObject);
            }
        }
        
        private void InitializeAudioSources()
        {
            if (!weaponAudioSource)
            {
                weaponAudioSource = gameObject.AddComponent<AudioSource>();
                weaponAudioSource.playOnAwake = false;
            }
            
            if (!uiAudioSource)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
            }
            
            if (!ambientAudioSource)
            {
                ambientAudioSource = gameObject.AddComponent<AudioSource>();
                ambientAudioSource.playOnAwake = false;
                ambientAudioSource.loop = true;
            }
        }
        
        // Weapon audio methods
        public void PlayWeaponFire(int weaponType = 0)
        {
            if (fireSounds != null && fireSounds.Length > weaponType && fireSounds[weaponType])
            {
                weaponAudioSource.PlayOneShot(fireSounds[weaponType]);
            }
        }
        
        public void PlayWeaponReload(int weaponType = 0)
        {
            if (reloadSounds != null && reloadSounds.Length > weaponType && reloadSounds[weaponType])
            {
                weaponAudioSource.PlayOneShot(reloadSounds[weaponType]);
            }
        }
        
        public void PlayImpact(int impactType = 0)
        {
            if (impactSounds != null && impactSounds.Length > impactType && impactSounds[impactType])
            {
                weaponAudioSource.PlayOneShot(impactSounds[impactType]);
            }
        }
        
        // Generic audio methods
        public void PlayOneShot(AudioClip clip, AudioSource source = null)
        {
            if (!clip) return;
            
            if (source == null) source = weaponAudioSource;
            source.PlayOneShot(clip);
        }
        
        public void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }
        
        // Volume controls
        public void SetWeaponVolume(float volume)
        {
            if (weaponAudioSource) weaponAudioSource.volume = Mathf.Clamp01(volume);
        }
        
        public void SetUIVolume(float volume)
        {
            if (uiAudioSource) uiAudioSource.volume = Mathf.Clamp01(volume);
        }
        
        public void SetAmbientVolume(float volume)
        {
            if (ambientAudioSource) ambientAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
}