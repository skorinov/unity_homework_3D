using Constants;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Settings UI controller
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        [Header("Audio Controls")]
        [SerializeField] private Button soundOnButton;
        [SerializeField] private Button soundOffButton;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        
        private bool _isSoundEnabled = true;
        
        private void Start()
        {
            LoadSettings();
            SetupEventListeners();
        }
        
        private void LoadSettings()
        {
            // Load saved settings
            _isSoundEnabled = PlayerPrefs.GetInt(GameConstants.PlayerPrefs.MASTER_SOUND_KEY, 1) == 1;
            float musicVolume = PlayerPrefs.GetFloat(GameConstants.PlayerPrefs.MUSIC_VOLUME_KEY, 0.75f);
            float sfxVolume = PlayerPrefs.GetFloat(GameConstants.PlayerPrefs.SFX_VOLUME_KEY, 0.75f);
            
            // Update UI elements
            if (musicVolumeSlider) musicVolumeSlider.value = musicVolume;
            if (sfxVolumeSlider) sfxVolumeSlider.value = sfxVolume;
            
            // Apply settings to AudioManager
            if (Managers.AudioManager.Instance)
            {
                Managers.AudioManager.Instance.SetMasterEnabled(_isSoundEnabled);
                Managers.AudioManager.Instance.SetMusicVolume(musicVolume);
                Managers.AudioManager.Instance.SetSFXVolume(sfxVolume);
            }
            
            // Update button states
            UpdateButtonStates();
            UpdateSliderInteractability();
        }
        
        private void SetupEventListeners()
        {
            if (soundOnButton)
                soundOnButton.onClick.AddListener(DisableSound);
            
            if (soundOffButton)
                soundOffButton.onClick.AddListener(EnableSound);
            
            if (musicVolumeSlider)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            if (sfxVolumeSlider)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        public void EnableSound()
        {
            _isSoundEnabled = true;
            
            // Update AudioManager
            if (Managers.AudioManager.Instance)
                Managers.AudioManager.Instance.SetMasterEnabled(true);
            
            // Update UI
            UpdateButtonStates();
            UpdateSliderInteractability();
            
            // Save setting
            PlayerPrefs.SetInt(GameConstants.PlayerPrefs.MASTER_SOUND_KEY, 1);
            PlayerPrefs.Save();
        }
        
        public void DisableSound()
        {
            _isSoundEnabled = false;
            
            // Update AudioManager
            if (Managers.AudioManager.Instance)
                Managers.AudioManager.Instance.SetMasterEnabled(false);
            
            // Update UI
            UpdateButtonStates();
            UpdateSliderInteractability();
            
            // Save setting
            PlayerPrefs.SetInt(GameConstants.PlayerPrefs.MASTER_SOUND_KEY, 0);
            PlayerPrefs.Save();
        }
        
        public void OnMusicVolumeChanged(float volume)
        {
            // Update AudioManager
            if (Managers.AudioManager.Instance)
                Managers.AudioManager.Instance.SetMusicVolume(volume);
            
            // Save setting
            PlayerPrefs.SetFloat(GameConstants.PlayerPrefs.MUSIC_VOLUME_KEY, volume);
        }
        
        public void OnSFXVolumeChanged(float volume)
        {
            // Update AudioManager
            if (Managers.AudioManager.Instance)
                Managers.AudioManager.Instance.SetSFXVolume(volume);
            
            // Save setting
            PlayerPrefs.SetFloat(GameConstants.PlayerPrefs.SFX_VOLUME_KEY, volume);
        }
        
        private void UpdateButtonStates()
        {
            if (_isSoundEnabled)
            {
                soundOnButton?.gameObject?.SetActive(true);
                soundOffButton?.gameObject?.SetActive(false);
            }
            else
            {
                soundOffButton?.gameObject?.SetActive(true);
                soundOnButton?.gameObject?.SetActive(false);
            }
        }
        
        private void UpdateSliderInteractability()
        {
            if (musicVolumeSlider)
                musicVolumeSlider.interactable = _isSoundEnabled;
            if (sfxVolumeSlider)
                sfxVolumeSlider.interactable = _isSoundEnabled;
        }
        
        // Public methods for external use
        public void ResetToDefaults()
        {
            _isSoundEnabled = true;
            if (musicVolumeSlider) musicVolumeSlider.value = 0.75f;
            if (sfxVolumeSlider) sfxVolumeSlider.value = 0.75f;
            UpdateButtonStates();
            UpdateSliderInteractability();
        }
        
        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }
    }
}