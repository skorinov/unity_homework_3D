using UnityEngine;
using Collectibles;
using Weapons;

namespace Player
{
    /// <summary>
    /// Manages weapon pickup, drop, and usage
    /// </summary>
    public class WeaponOwner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private float pickupRange = 4f;
        [SerializeField] private float crosshairRadius = 100f;
        [SerializeField] private float dropForce = 5f;
        [SerializeField] private LayerMask weaponLayerMask = -1;
        
        private Weapon currentWeapon;
        private CollectibleWeapon currentCollectible;
        private CollectibleWeapon _highlightedWeapon;
        private Camera playerCamera;
        
        public Weapon CurrentWeapon => currentWeapon;
        public bool HasWeapon => currentWeapon != null;
        public CollectibleWeapon HighlightedWeapon => _highlightedWeapon;
        
        private void Start()
        {
            playerCamera = Camera.main;
            
            // Check if player starts with a weapon
            if (weaponHolder && weaponHolder.childCount > 0)
            {
                var startingWeapon = weaponHolder.GetComponentInChildren<Weapon>();
                if (startingWeapon)
                {
                    currentWeapon = startingWeapon;
                    currentCollectible = startingWeapon.GetComponent<CollectibleWeapon>();
                    if (currentCollectible)
                        currentCollectible.Attach(weaponHolder);
                }
            }
        }
        
        private void Update()
        {
            UpdateWeaponHighlight();
        }
        
        private void UpdateWeaponHighlight()
        {
            var targetWeapon = GetWeaponNearCrosshair();
            
            if (targetWeapon != _highlightedWeapon)
            {
                // Remove old highlight
                if (_highlightedWeapon)
                    _highlightedWeapon.SetHighlight(false);
                
                // Add new highlight
                _highlightedWeapon = targetWeapon;
                if (_highlightedWeapon)
                    _highlightedWeapon.SetHighlight(true);
            }
        }
        
        private CollectibleWeapon GetWeaponNearCrosshair()
        {
            if (!playerCamera) return null;
            
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            
            // Find all weapons in range
            Collider[] weaponsInRange = Physics.OverlapSphere(transform.position, pickupRange, weaponLayerMask);
            
            CollectibleWeapon closestToCrosshair = null;
            float closestDistance = float.MaxValue;
            
            foreach (var weaponCollider in weaponsInRange)
            {
                var weapon = weaponCollider.GetComponent<CollectibleWeapon>();
                if (!weapon || weapon.IsAttached) continue;
                
                // Convert weapon position to screen space
                Vector3 screenPos = playerCamera.WorldToScreenPoint(weapon.transform.position);
                
                // Check if weapon is in front of camera
                if (screenPos.z <= 0) continue;
                
                // Calculate distance from screen center
                float screenDistance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), 
                                                       new Vector2(screenCenter.x, screenCenter.y));
                
                // Check if weapon is within crosshair radius
                if (screenDistance <= crosshairRadius && screenDistance < closestDistance)
                {
                    // Additional raycast check for line of sight
                    Vector3 directionToWeapon = (weapon.transform.position - playerCamera.transform.position).normalized;
                    float distanceToWeapon = Vector3.Distance(playerCamera.transform.position, weapon.transform.position);
                    
                    if (Physics.Raycast(playerCamera.transform.position, directionToWeapon, out RaycastHit hit, distanceToWeapon + 0.5f))
                    {
                        if (hit.collider == weaponCollider)
                        {
                            closestDistance = screenDistance;
                            closestToCrosshair = weapon;
                        }
                    }
                }
            }
            
            return closestToCrosshair;
        }
        
        public void TryPickupWeapon()
        {
            var weaponToPickup = _highlightedWeapon;
            
            if (!weaponToPickup)
                weaponToPickup = FindNearestWeapon();
                
            if (weaponToPickup)
            {
                PickupWeapon(weaponToPickup);
            }
        }
        
        public void PickupWeapon(CollectibleWeapon weapon)
        {
            if (!weapon || weapon.IsAttached) return;
            
            // Remove highlight
            if (weapon == _highlightedWeapon)
            {
                weapon.SetHighlight(false);
                _highlightedWeapon = null;
            }
            
            // Drop current weapon
            if (HasWeapon)
                DropCurrentWeapon();
            
            // Pickup new weapon
            weapon.Attach(weaponHolder);
            currentCollectible = weapon;
            currentWeapon = weapon.WeaponComponent;
        }
        
        public void DropCurrentWeapon()
        {
            if (!HasWeapon) return;
            
            if (currentCollectible)
            {
                currentCollectible.Detach();
                
                // Add extra drop force
                var rb = currentCollectible.GetComponent<Rigidbody>();
                if (rb)
                {
                    Vector3 dropDirection = (transform.forward + Vector3.up * 0.3f).normalized;
                    rb.AddForce(dropDirection * dropForce, ForceMode.Impulse);
                }
            }
            
            currentWeapon = null;
            currentCollectible = null;
        }
        
        public void Fire()
        {
            if (currentWeapon)
            {
                currentWeapon.Fire();
            }
        }
        
        public void StartFiring()
        {
            if (currentWeapon)
            {
                currentWeapon.StartFiring();
            }
        }
        
        public void StopFiring()
        {
            if (currentWeapon)
            {
                currentWeapon.StopFiring();
            }
        }
        
        public void Reload()
        {
            if (currentWeapon)
            {
                currentWeapon.StartReload();
            }
        }
        
        public bool CanFire()
        {
            return currentWeapon?.CanFire ?? false;
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