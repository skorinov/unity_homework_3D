using UnityEngine;
using Weapons;

namespace AI
{
    /// <summary>
    /// Enemy weapon controller with burst firing and target provider
    /// </summary>
    public class EnemyWeaponController : MonoBehaviour, IWeaponUser, ITargetProvider
    {
        [Header("Weapon Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private float burstCount = 3;
        [SerializeField] private float burstDelay = 0.1f;
        [SerializeField] private LayerMask targetLayers = -1;
        [SerializeField] private float aimHeight = 1.5f;
        
        private Transform _target;
        private Weapon _weapon;
        private bool _isFiring = false;
        private int _currentBurstShot = 0;
        private float _lastBurstTime;
        
        // IWeaponUser implementation
        public Transform FirePoint => firePoint ? firePoint : transform;
        public Camera UserCamera => null;
        public bool CanUseWeapon => enabled && gameObject.activeInHierarchy;
        public LayerMask TargetLayers => targetLayers;
        public ITargetProvider TargetProvider => this;
        public bool HasInfiniteAmmo => true;
        
        // ITargetProvider implementation
        public Transform GetTarget() => _target;
        public Vector3 GetAimPoint() => _target ? _target.position + Vector3.up * aimHeight : Vector3.zero;
        public bool HasValidTarget() => _target != null;
        
        // Public properties
        public bool CanShoot => _weapon?.CanFire == true && HasValidTarget() && HasLineOfSight();
        public bool IsFiring => _isFiring;
        
        private void Awake()
        {
            FindWeapon();
        }
        
        private void Start()
        {
            if (_weapon)
            {
                _weapon.SetWeaponUser(this);
            }
        }
        
        private void Update()
        {
            AutoFindTarget();
        }
        
        private void FindWeapon()
        {
            _weapon = GetComponentInChildren<Weapon>(true);
            
            if (!_weapon)
            {
                Debug.LogError($"[{gameObject.name}] No Weapon component found in children!");
            }
        }
        
        private void AutoFindTarget()
        {
            if (!_target)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player)
                {
                    SetTarget(player.transform);
                }
            }
        }
        
        public void SetTarget(Transform target)
        {
            _target = target;
        }
        
        public void StartFiring()
        {
            if (!CanShoot || _isFiring) return;
            
            if (Time.time >= _lastBurstTime + (60f / _weapon.weaponData.fireRate))
            {
                _isFiring = true;
                _currentBurstShot = 0;
                StartCoroutine(FireBurst());
            }
        }
        
        public void StopFiring()
        {
            if (_isFiring)
            {
                _isFiring = false;
                StopAllCoroutines();
            }
        }
        
        private System.Collections.IEnumerator FireBurst()
        {
            while (_isFiring && _currentBurstShot < burstCount)
            {
                if (CanShoot)
                {
                    _weapon.Fire();
                    _currentBurstShot++;
                    
                    if (_currentBurstShot < burstCount)
                        yield return new WaitForSeconds(burstDelay);
                }
                else
                {
                    break;
                }
            }
            
            _lastBurstTime = Time.time;
            _isFiring = false;
        }
        
        private bool HasLineOfSight(float maxDistance = 50f)
        {
            if (!HasValidTarget()) return false;
            
            Vector3 startPos = FirePoint.position;
            Vector3 targetPos = GetAimPoint();
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, Mathf.Min(distance, maxDistance)))
            {
                return hit.transform == _target || 
                       hit.transform.IsChildOf(_target) || 
                       hit.transform.CompareTag("Player");
            }
            
            return true;
        }
        
        // IWeaponUser callbacks
        public void OnWeaponFired()
        {
            // Enemy-specific fire feedback
        }
        
        public void OnWeaponReloaded()
        {
            // Enemies don't reload with infinite ammo
        }
    }
}