using Constants;
using UnityEngine;
using Collectibles;
using Managers;
using Weapons;

namespace Player
{
    /// <summary>
    /// Player weapon owner with pickup system
    /// </summary>
    public class WeaponOwner : MonoBehaviour, IWeaponUser
    {
        [Header("Settings")]
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private float pickupRange = 3f;
        [SerializeField] private float crosshairRadius = 100f;
        [SerializeField] private float dropForce = 5f;
        [SerializeField] private LayerMask weaponLayerMask = -1;
        [SerializeField] private LayerMask targetLayers = -1;
        
        private Weapon _currentWeapon;
        private CollectibleWeapon _currentCollectible;
        private CollectibleWeapon _highlightedWeapon;
        private Camera _playerCamera;
        
        // Cached values for performance
        private Vector3 _screenCenter;
        private bool _screenCenterCached;
        
        // IWeaponUser properties
        public Transform FirePoint => weaponHolder;
        public Camera UserCamera => _playerCamera;
        public bool CanUseWeapon => enabled && gameObject.activeInHierarchy;
        public LayerMask TargetLayers => targetLayers;
        public ITargetProvider TargetProvider => null; // Players don't use target providers
        public bool HasInfiniteAmmo => true;
        
        // Public properties
        public Weapon CurrentWeapon => _currentWeapon;
        public bool HasWeapon => _currentWeapon != null;
        public CollectibleWeapon HighlightedWeapon => _highlightedWeapon;
        
        private void Start()
        {
            _playerCamera = Camera.main;
            CacheScreenCenter();
            SetupStartingWeapon();
        }
        
        private void Update()
        {
            UpdateWeaponHighlight();
        }
        
        private void CacheScreenCenter()
        {
            if (!_screenCenterCached)
            {
                _screenCenter = new Vector3(
                    Screen.width * GameConstants.Weapons.SCREEN_CENTER_X, 
                    Screen.height * GameConstants.Weapons.SCREEN_CENTER_Y, 
                    0f);
                _screenCenterCached = true;
            }
        }
        
        private void SetupStartingWeapon()
        {
            if (weaponHolder && weaponHolder.childCount > 0)
            {
                var startingWeapon = weaponHolder.GetComponentInChildren<Weapon>();
                if (startingWeapon)
                {
                    _currentWeapon = startingWeapon;
                    _currentWeapon.SetWeaponUser(this);
                    _currentCollectible = startingWeapon.GetComponent<CollectibleWeapon>();
                    _currentCollectible?.Attach(weaponHolder);
                }
            }
        }
        
        public void OnWeaponFired()
        {
            // Player-specific fire feedback can be added here
        }
        
        public void OnWeaponReloaded()
        {
            // Player-specific reload feedback can be added here
        }
        
        private void UpdateWeaponHighlight()
        {
            var targetWeapon = GetWeaponNearCrosshair();
            
            if (targetWeapon != _highlightedWeapon)
            {
                if (_highlightedWeapon)
                    _highlightedWeapon.SetHighlight(false);
                
                _highlightedWeapon = targetWeapon;
                if (_highlightedWeapon)
                    _highlightedWeapon.SetHighlight(true);
            }
        }
        
        private CollectibleWeapon GetWeaponNearCrosshair()
        {
            if (!_playerCamera) return null;
            
            Collider[] weaponsInRange = Physics.OverlapSphere(transform.position, pickupRange, weaponLayerMask);
            
            CollectibleWeapon closestToCrosshair = null;
            float closestDistance = float.MaxValue;
            
            foreach (var weaponCollider in weaponsInRange)
            {
                var weapon = weaponCollider.GetComponent<CollectibleWeapon>();
                if (!weapon || weapon.IsAttached) continue;
                
                Vector3 screenPos = _playerCamera.WorldToScreenPoint(weapon.transform.position);
                
                if (screenPos.z <= 0) continue; // Behind camera
                
                float screenDistance = Vector2.Distance(
                    new Vector2(screenPos.x, screenPos.y), 
                    new Vector2(_screenCenter.x, _screenCenter.y));
                
                if (screenDistance <= crosshairRadius && screenDistance < closestDistance)
                {
                    if (HasLineOfSightToWeapon(weapon, weaponCollider))
                    {
                        closestDistance = screenDistance;
                        closestToCrosshair = weapon;
                    }
                }
            }
            
            return closestToCrosshair;
        }
        
        private bool HasLineOfSightToWeapon(CollectibleWeapon weapon, Collider weaponCollider)
        {
            Vector3 directionToWeapon = (weapon.transform.position - _playerCamera.transform.position).normalized;
            float distanceToWeapon = Vector3.Distance(_playerCamera.transform.position, weapon.transform.position);
            
            if (Physics.Raycast(_playerCamera.transform.position, directionToWeapon, out RaycastHit hit, distanceToWeapon + 0.5f))
            {
                return hit.collider == weaponCollider;
            }
            
            return true;
        }
        
        public void TryPickupWeapon()
        {
            var weaponToPickup = _highlightedWeapon ?? FindNearestWeapon();
            
            if (weaponToPickup)
                PickupWeapon(weaponToPickup);
        }
        
        public void PickupWeapon(CollectibleWeapon weapon)
        {
            if (!weapon || weapon.IsAttached) return;
            
            // Clear highlight
            if (weapon == _highlightedWeapon)
            {
                weapon.SetHighlight(false);
                _highlightedWeapon = null;
            }
            
            // Drop current weapon if any
            if (HasWeapon)
                DropCurrentWeapon();
            
            // Pickup new weapon
            weapon.Attach(weaponHolder);
            _currentCollectible = weapon;
            _currentWeapon = weapon.WeaponComponent;
            
            AudioManager.Instance?.PlayWeaponSwitch();
            
            if (_currentWeapon)
                _currentWeapon.SetWeaponUser(this);
        }
        
        public void DropCurrentWeapon()
        {
            if (!HasWeapon) return;
            
            if (_currentCollectible)
            {
                _currentCollectible.Detach();
                
                // Add drop physics
                var rb = _currentCollectible.GetComponent<Rigidbody>();
                if (rb)
                {
                    Vector3 dropDirection = (transform.forward + Vector3.up * 0.3f).normalized;
                    rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
                }
            }
            
            _currentWeapon = null;
            _currentCollectible = null;
        }
        
        public void Fire()
        {
            _currentWeapon?.Fire();
        }
        
        public void StartFiring()
        {
            _currentWeapon?.StartFiring();
        }
        
        public void StopFiring()
        {
            _currentWeapon?.StopFiring();
        }
        
        public void Reload()
        {
            _currentWeapon?.StartReload();
        }
        
        public bool CanFire()
        {
            return _currentWeapon?.CanFire ?? false;
        }
        
        private CollectibleWeapon FindNearestWeapon()
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, pickupRange, weaponLayerMask);
            
            CollectibleWeapon nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in nearby)
            {
                var weapon = collider.GetComponent<CollectibleWeapon>();
                if (weapon && !weapon.IsAttached)
                {
                    float distance = Vector3.Distance(transform.position, weapon.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = weapon;
                    }
                }
            }
            
            return nearest;
        }
    }
}