using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Simple game UI with health bar, health text, and crosshair
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Health UI - Image Based")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Header("Crosshair")]
        [SerializeField] private GameObject crosshair;
        
        private Player.PlayerHealth _playerHealth;
        
        private void Start()
        {
            FindPlayerHealth();
            
            // Subscribe to health changes and set initial values
            if (_playerHealth)
            {
                _playerHealth.OnHealthChanged += UpdateHealthDisplay;
                // Set initial health display immediately
                UpdateHealthDisplay(_playerHealth.Health);
            }
            
            // Show crosshair
            SetCrosshairVisible(true);
        }
        
        private void FindPlayerHealth()
        {
            _playerHealth = FindFirstObjectByType<Player.PlayerHealth>();
            
            if (!_playerHealth)
            {
                Debug.LogWarning("[GameUI] PlayerHealth component not found!");
            }
        }
        
        private void UpdateHealthDisplay(float newHealth)
        {
            if (!_playerHealth) return;
            
            float healthRatio = newHealth / _playerHealth.MaxHealth;
            
            // Update health bar fill amount (0 = empty, 1 = full)
            if (healthBarFill)
            {
                healthBarFill.fillAmount = healthRatio;
            }
            
            // Update health text
            UpdateHealthText(newHealth);
        }
        
        private void UpdateHealthText(float currentHealth)
        {
            if (!_playerHealth) return;
            
            int currentHP = Mathf.CeilToInt(currentHealth);
            string healthString = currentHP.ToString();
            
            // Update TextMeshPro if available
            if (healthText)
            {
                healthText.text = healthString;
            }
        }
        
        public void SetCrosshairVisible(bool visible)
        {
            if (crosshair)
                crosshair.SetActive(visible);
        }
        
        private void OnDestroy()
        {
            if (_playerHealth)
                _playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
}