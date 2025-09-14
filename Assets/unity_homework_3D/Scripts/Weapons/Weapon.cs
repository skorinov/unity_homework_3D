using Constants;
using Managers;
using UnityEngine;

[System.Serializable]
public struct WeaponData
{
    public string weaponName;
    public float damage;
    public float fireRate;
    public float range;
    public int maxAmmo;
    public float reloadTime;
    public float bulletSpeed;
    public float spread;
    public bool isFullAuto;
    public bool isShotgun;
    public int pelletCount;
    public float pelletSpread;
}

namespace Weapons
{
    /// <summary>
    /// Universal weapon system with infinite ammo support for AI
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        public WeaponData weaponData = new WeaponData 
        { 
            weaponName = "Weapon",
            damage = GameConstants.Weapons.DEFAULT_DAMAGE, 
            fireRate = GameConstants.Weapons.DEFAULT_FIRE_RATE, 
            range = GameConstants.Weapons.DEFAULT_RANGE, 
            maxAmmo = GameConstants.Weapons.DEFAULT_MAX_AMMO, 
            reloadTime = GameConstants.Weapons.DEFAULT_RELOAD_TIME, 
            bulletSpeed = GameConstants.Bullets.DEFAULT_SPEED, 
            spread = GameConstants.Weapons.DEFAULT_SPREAD,
            isFullAuto = false,
            isShotgun = false,
            pelletCount = 8,
            pelletSpread = 0.15f
        };
        
        [Header("References")]
        public Transform firePoint;
        
        // Runtime state
        private int _currentAmmo;
        private float _nextFireTime;
        private bool _isReloading;
        private bool _isFullAutoFiring;
        private IWeaponUser _weaponUser;
        private GameObject _actualOwner;
        
        // Cached values for performance
        private Vector3 _screenCenter;
        private bool _screenCenterCached;
        
        // Properties
        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => weaponData.maxAmmo;
        public bool IsReloading => _isReloading;
        public bool CanFire => !_isReloading && Time.time >= _nextFireTime && HasAmmo() && _weaponUser?.CanUseWeapon == true;
        public string WeaponName => weaponData.weaponName;
        public bool IsFullAuto => weaponData.isFullAuto;
        public IWeaponUser WeaponUser => _weaponUser;
        
        private bool HasAmmo() => _weaponUser?.HasInfiniteAmmo == true || _currentAmmo > 0;
        
        private void Start()
        {
            _currentAmmo = weaponData.maxAmmo;
            CacheScreenCenter();
        }
        
        private void Update()
        {
            if (weaponData.isFullAuto && _isFullAutoFiring && CanFire)
                Fire();
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
        
        public void SetWeaponUser(IWeaponUser user)
        {
            _weaponUser = user;
            _actualOwner = (user as MonoBehaviour)?.gameObject;
        }
        
        public virtual void Fire()
        {
            if (!CanFire) return;

            // Consume ammo only if not infinite
            if (_weaponUser?.HasInfiniteAmmo != true)
                _currentAmmo--;
                
            _nextFireTime = Time.time + (60f / weaponData.fireRate);

            if (weaponData.isShotgun)
                FireShotgun();
            else
                FireSingleBullet();
            
            PlayEffects();
            _weaponUser?.OnWeaponFired();
        }
        
        private void FireSingleBullet()
        {
            Vector3 targetPoint = GetTargetPoint();
            CreateBullet(targetPoint);
            CreateBulletTrail(GetFirePoint().position, targetPoint);
        }
        
        private void FireShotgun()
        {
            Vector3 centerTarget = GetTargetPoint();
            
            for (int i = 0; i < weaponData.pelletCount; i++)
            {
                CreateBullet(GetShotgunTargetPoint());
            }
            
            CreateBulletTrail(GetFirePoint().position, centerTarget);
        }
        
        private Vector3 GetTargetPoint()
        {
            var targetProvider = _weaponUser?.TargetProvider;
            if (targetProvider?.HasValidTarget() == true)
            {
                return GetDirectTargetPoint(targetProvider);
            }
            
            var userCamera = _weaponUser?.UserCamera;
            if (userCamera)
            {
                Ray cameraRay = userCamera.ScreenPointToRay(_screenCenter);
                Vector3 shootDirection = AddSpread(cameraRay.direction);
                Ray spreadRay = new Ray(cameraRay.origin, shootDirection);
                
                LayerMask hitLayers = _weaponUser?.TargetLayers ?? GameConstants.Bullets.DEFAULT_HIT_LAYERS;
                if (Physics.Raycast(spreadRay, out RaycastHit hit, weaponData.range, hitLayers))
                    return hit.point;
                
                return spreadRay.origin + spreadRay.direction * weaponData.range;
            }
            
            Transform firePoint = GetFirePoint();
            return firePoint.position + firePoint.forward * weaponData.range;
        }
        
        private Vector3 GetDirectTargetPoint(ITargetProvider targetProvider)
        {
            Transform firePoint = GetFirePoint();
            Vector3 targetPos = targetProvider.GetAimPoint();
            
            Vector3 aimDirection = (targetPos - firePoint.position).normalized;
            aimDirection = AddSpread(aimDirection);
            
            LayerMask hitLayers = _weaponUser?.TargetLayers ?? GameConstants.Bullets.DEFAULT_HIT_LAYERS;
            if (Physics.Raycast(firePoint.position, aimDirection, out RaycastHit hit, weaponData.range, hitLayers))
                return hit.point;
            
            return firePoint.position + aimDirection * weaponData.range;
        }
        
        private Vector3 GetShotgunTargetPoint()
        {
            var targetProvider = _weaponUser?.TargetProvider;
            if (targetProvider?.HasValidTarget() == true)
            {
                Vector3 baseTarget = targetProvider.GetAimPoint();
                Vector3 shotgunDirection = (baseTarget - GetFirePoint().position).normalized;
                return GetFirePoint().position + AddShotgunSpread(shotgunDirection) * weaponData.range;
            }
            
            var userCamera = _weaponUser?.UserCamera;
            if (userCamera)
            {
                Ray cameraRay = userCamera.ScreenPointToRay(_screenCenter);
                Vector3 pelletDirection = AddShotgunSpread(cameraRay.direction);
                Ray spreadRay = new Ray(cameraRay.origin, pelletDirection);
                
                LayerMask hitLayers = _weaponUser?.TargetLayers ?? GameConstants.Bullets.DEFAULT_HIT_LAYERS;
                if (Physics.Raycast(spreadRay, out RaycastHit hit, weaponData.range, hitLayers))
                    return hit.point;
                
                return spreadRay.origin + spreadRay.direction * weaponData.range;
            }
            
            return GetFirePoint().position + GetFirePoint().forward * weaponData.range;
        }
        
        private Vector3 AddSpread(Vector3 baseDirection)
        {
            var userCamera = _weaponUser?.UserCamera;
            if (!userCamera) 
            {
                Vector3 randomSpread = Random.insideUnitSphere * weaponData.spread;
                return (baseDirection + randomSpread).normalized;
            }
            
            Vector3 right = userCamera.transform.right;
            Vector3 up = userCamera.transform.up;
            
            Vector3 spreadVector = right * Random.Range(-weaponData.spread, weaponData.spread) +
                                 up * Random.Range(-weaponData.spread, weaponData.spread);
            
            return (baseDirection + spreadVector).normalized;
        }
        
        private Vector3 AddShotgunSpread(Vector3 baseDirection)
        {
            var userCamera = _weaponUser?.UserCamera;
            if (!userCamera)
            {
                Vector3 randomSpread = Random.insideUnitSphere * weaponData.pelletSpread;
                return (baseDirection + randomSpread).normalized;
            }
            
            Vector3 right = userCamera.transform.right;
            Vector3 up = userCamera.transform.up;
            
            Vector3 spread = right * Random.Range(-weaponData.pelletSpread, weaponData.pelletSpread) +
                           up * Random.Range(-weaponData.pelletSpread, weaponData.pelletSpread);
            
            return (baseDirection + spread).normalized;
        }
        
        private void CreateBullet(Vector3 targetPoint)
        {
            Transform currentFirePoint = GetFirePoint();
            Vector3 bulletDirection = (targetPoint - currentFirePoint.position).normalized;
    
            // Determine if this is a player bullet based on weapon user type
            bool isPlayerBullet = _weaponUser is Player.WeaponOwner; // Direct type check
    
            ObjectPool.Instance?.SpawnFromPool<PhysicsBullet>(
                GameConstants.Pools.BULLET, 
                currentFirePoint.position, 
                Quaternion.LookRotation(bulletDirection),
                bullet => bullet.Initialize(
                    targetPoint, 
                    weaponData.bulletSpeed, 
                    weaponData.damage, 
                    isPlayerBullet
                )
            );
        }
        
        private void CreateBulletTrail(Vector3 startPoint, Vector3 endPoint)
        {
            ObjectPool.Instance?.SpawnFromPool<BulletTrail>(
                GameConstants.Pools.BULLET_TRAIL, 
                Vector3.zero, 
                Quaternion.identity,
                trail => trail.Initialize(startPoint, endPoint)
            );
        }
        
        private void PlayMuzzleFlash()
        {
            Transform currentFirePoint = GetFirePoint();
            ObjectPool.Instance?.SpawnFromPoolTimed(
                GameConstants.Pools.MUZZLE_FLASH, 
                currentFirePoint.position, 
                currentFirePoint.rotation,
                GameConstants.Weapons.MUZZLE_FLASH_LIFETIME
            )?.transform.SetParent(currentFirePoint);
        }
        
        private void PlayEffects()
        {
            AudioManager.Instance?.PlayWeaponFire();
            PlayMuzzleFlash();
        }
        
        private Transform GetFirePoint() => firePoint ? firePoint : (_weaponUser?.FirePoint ?? transform);
        
        public void StartFiring()
        {
            if (weaponData.isFullAuto)
                _isFullAutoFiring = true;
        }
        
        public void StopFiring() => _isFullAutoFiring = false;
        
        public virtual void StartReload()
        {
            // Skip reload if infinite ammo or already full
            if (_weaponUser?.HasInfiniteAmmo == true || _isReloading || _currentAmmo == weaponData.maxAmmo) 
                return;
                
            StartCoroutine(ReloadCoroutine());
        }
        
        private System.Collections.IEnumerator ReloadCoroutine()
        {
            _isReloading = true;
            
            AudioManager.Instance?.PlayWeaponReload();
            
            yield return new WaitForSeconds(weaponData.reloadTime);
            
            _currentAmmo = weaponData.maxAmmo;
            _isReloading = false;
            
            _weaponUser?.OnWeaponReloaded();
        }
    }
}