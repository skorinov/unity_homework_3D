using Constants;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Player;

namespace UI
{
    /// <summary>
    /// UI manager for health display with smooth animations
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private TextMeshProUGUI regenIndicator;
        
        [Header("Color Settings")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.yellow;
        [SerializeField] private Color criticalHealthColor = Color.red;
        [SerializeField] private float criticalHealthThreshold = GameConstants.Health.LOW_HEALTH_THRESHOLD;
        [SerializeField] private float lowHealthThreshold = 0.5f;
        
        [Header("Animation Settings")]
        [SerializeField] private bool animateHealthBar = true;
        [SerializeField] private float animationSpeed = 2f;
        
        private PlayerHealth _playerHealth;
        private float _targetHealthValue;
        private float _currentDisplayedHealth;
        
        private void Start()
        {
            FindPlayerHealth();
            
            if (_playerHealth)
            {
                _playerHealth.OnHealthChanged += OnHealthChanged;
                _targetHealthValue = _playerHealth.Health / _playerHealth.MaxHealth;
                _currentDisplayedHealth = _targetHealthValue;
                
                InitializeUI();
            }
        }
        
        private void Update()
        {
            if (_playerHealth)
            {
                if (animateHealthBar)
                    UpdateHealthBarAnimation();
                
                UpdateRegenIndicator();
            }
        }
        
        private void FindPlayerHealth()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            
            if (!_playerHealth)
            {
                Debug.LogWarning("[HealthUI] PlayerHealth component not found!");
            }
        }
        
        private void InitializeUI()
        {
            if (healthBar)
                healthBar.value = _targetHealthValue;
            
            UpdateHealthText();
            UpdateHealthBarColor();
        }
        
        private void OnHealthChanged(float newHealth)
        {
            _targetHealthValue = newHealth / _playerHealth.MaxHealth;
            
            if (!animateHealthBar)
            {
                _currentDisplayedHealth = _targetHealthValue;
                if (healthBar)
                    healthBar.value = _currentDisplayedHealth;
            }
            
            UpdateHealthText();
            UpdateHealthBarColor();
        }
        
        private void UpdateHealthBarAnimation()
        {
            if (Mathf.Abs(_currentDisplayedHealth - _targetHealthValue) > 0.01f)
            {
                _currentDisplayedHealth = Mathf.Lerp(_currentDisplayedHealth, _targetHealthValue, 
                    animationSpeed * Time.deltaTime);
                
                if (healthBar)
                    healthBar.value = _currentDisplayedHealth;
            }
        }
        
        private void UpdateHealthText()
        {
            if (healthText && _playerHealth)
            {
                healthText.text = $"{Mathf.Ceil(_playerHealth.Health)}/{_playerHealth.MaxHealth}";
            }
        }
        
        private void UpdateHealthBarColor()
        {
            if (!healthBarFill) return;
            
            Color targetColor;
            
            if (_targetHealthValue <= criticalHealthThreshold)
            {
                targetColor = criticalHealthColor;
            }
            else if (_targetHealthValue <= lowHealthThreshold)
            {
                float t = (_targetHealthValue - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold);
                targetColor = Color.Lerp(criticalHealthColor, lowHealthColor, t);
            }
            else
            {
                float t = (_targetHealthValue - lowHealthThreshold) / (1f - lowHealthThreshold);
                targetColor = Color.Lerp(lowHealthColor, healthyColor, t);
            }
            
            healthBarFill.color = targetColor;
        }
        
        private void UpdateRegenIndicator()
        {
            if (!regenIndicator || !_playerHealth) return;
            
            if (_playerHealth.IsRegenerating)
            {
                if (!regenIndicator.gameObject.activeSelf)
                    regenIndicator.gameObject.SetActive(true);
                
                // Simple pulse animation
                float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                regenIndicator.alpha = 0.5f + pulse * 0.5f;
            }
            else if (regenIndicator.gameObject.activeSelf)
            {
                regenIndicator.gameObject.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (_playerHealth)
            {
                _playerHealth.OnHealthChanged -= OnHealthChanged;
            }
        }
    }
}