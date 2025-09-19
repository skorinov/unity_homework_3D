using Constants;
using UnityEngine;
using UnityEngine.UI;
using Weapons;

namespace Player
{
    /// <summary>
    /// Player health system with regeneration and UI feedback
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = GameConstants.Health.DEFAULT_PLAYER_HEALTH;
        [SerializeField] private float healthRegenRate = GameConstants.Health.DEFAULT_REGEN_RATE;
        [SerializeField] private float regenDelay = GameConstants.Health.DEFAULT_REGEN_DELAY;
        
        [Header("Visual Effects")]
        [SerializeField] private Image damageOverlay;
        [SerializeField] private Color damageOverlayColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private float damageOverlayDuration = 0.5f;
        
        private float _currentHealth;
        private float _lastDamageTime;
        private bool _isRegenerating;
        private PlayerController _playerController;
        
        // Properties
        public float Health => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool IsRegenerating => _isRegenerating;
        
        // Events
        public System.Action<float> OnHealthChanged;
        public System.Action OnPlayerDied;
        
        private void Awake()
        {
            _currentHealth = maxHealth;
            _playerController = GetComponent<PlayerController>();
            
            SetupDamageOverlay();
        }
        
        private void Update()
        {
            if (IsAlive)
                HandleHealthRegeneration();
        }
        
        private void SetupDamageOverlay()
        {
            if (damageOverlay)
            {
                damageOverlay.color = Color.clear;
                damageOverlay.gameObject.SetActive(false);
            }
        }
        
        private void HandleHealthRegeneration()
        {
            if (_currentHealth < maxHealth && Time.time - _lastDamageTime >= regenDelay)
            {
                if (!_isRegenerating)
                    _isRegenerating = true;
                
                _currentHealth = Mathf.Min(maxHealth, _currentHealth + healthRegenRate * Time.deltaTime);
                OnHealthChanged?.Invoke(_currentHealth);
                
                if (_currentHealth >= maxHealth)
                    _isRegenerating = false;
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            _lastDamageTime = Time.time;
            _isRegenerating = false;
            
            ShowDamageEffect();
            OnHealthChanged?.Invoke(_currentHealth);
            
            if (_currentHealth <= 0)
                Die();
        }
        
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth);
        }
        
        public void SetMaxHealth(float newMaxHealth)
        {
            float healthPercentage = _currentHealth / maxHealth;
            maxHealth = newMaxHealth;
            _currentHealth = maxHealth * healthPercentage;
            OnHealthChanged?.Invoke(_currentHealth);
        }
        
        private void Die()
        {
            // Disable player controls
            if (_playerController)
                _playerController.enabled = false;
            
            OnPlayerDied?.Invoke();
        }
        
        private void ShowDamageEffect()
        {
            if (damageOverlay)
                StartCoroutine(DamageOverlayEffect());
        }
        
        private System.Collections.IEnumerator DamageOverlayEffect()
        {
            damageOverlay.gameObject.SetActive(true);
            damageOverlay.color = damageOverlayColor;
            
            float elapsedTime = 0f;
            while (elapsedTime < damageOverlayDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(damageOverlayColor.a, 0f, elapsedTime / damageOverlayDuration);
                
                Color currentColor = damageOverlay.color;
                currentColor.a = alpha;
                damageOverlay.color = currentColor;
                
                yield return null;
            }
            
            damageOverlay.gameObject.SetActive(false);
        }
        
        public void Respawn()
        {
            _currentHealth = maxHealth;
            _lastDamageTime = 0f;
            _isRegenerating = false;
            
            if (_playerController)
                _playerController.enabled = true;
            
            OnHealthChanged?.Invoke(_currentHealth);
        }
        
        // Utility methods
        public float GetHealthPercentage() => _currentHealth / maxHealth;
        
        public bool IsLowHealth(float threshold = GameConstants.Health.LOW_HEALTH_THRESHOLD) => GetHealthPercentage() <= threshold;
        
        public bool IsFullHealth() => _currentHealth >= maxHealth;
    }
}