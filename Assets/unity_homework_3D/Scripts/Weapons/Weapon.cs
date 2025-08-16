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

[System.Serializable]
public struct BulletData
{
    public float lifetime;
    public int maxBounces;
    public float bounceForce;
    public LayerMask hitLayers;
}

[System.Serializable]
public struct EffectData
{
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public GameObject decalPrefab;
    public GameObject bulletTrailPrefab;
    public float decalLifetime;
    public AudioClip fireSound;
    public AudioClip reloadSound;
}

namespace Weapons
{
    /// <summary>
    /// Simplified weapon system using direct configuration
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
        
        public BulletData bulletData = new BulletData 
        { 
            lifetime = GameConstants.Bullets.DEFAULT_LIFETIME, 
            maxBounces = GameConstants.Bullets.DEFAULT_MAX_BOUNCES, 
            bounceForce = GameConstants.Bullets.DEFAULT_BOUNCE_FORCE, 
            hitLayers = -1 
        };
        
        public EffectData effectData;
        
        [Header("References")]
        public Transform firePoint;
        public GameObject bulletPrefab;
        [SerializeField] private AudioSource audioSource;
        
        // Runtime state
        protected int _currentAmmo;
        protected float _nextFireTime;
        protected bool _isReloading;
        private bool _isFullAutoFiring;
        protected Camera _playerCamera;
        
        // Properties
        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => weaponData.maxAmmo;
        public bool IsReloading => _isReloading;
        public bool CanFire => !_isReloading && Time.time >= _nextFireTime && _currentAmmo > 0;
        public string WeaponName => weaponData.weaponName;
        public bool IsFullAuto => weaponData.isFullAuto;
        
        protected virtual void Start()
        {
            _currentAmmo = weaponData.maxAmmo;
            _playerCamera = Camera.main;
        }
        
        private void Update()
        {
            if (weaponData.isFullAuto && _isFullAutoFiring && CanFire)
                Fire();
        }
        
        public virtual void Fire()
        {
            if (!CanFire) return;

            _currentAmmo--;
            _nextFireTime = Time.time + (60f / weaponData.fireRate);

            if (weaponData.isShotgun)
                FireShotgun();
            else
                FireSingleBullet();
            
            PlayEffects();
        }
        
        private void FireSingleBullet()
        {
            Vector3 targetPoint = GetTargetPoint();
            CreateBullet(targetPoint);
            CreateBulletTrail(firePoint.position, targetPoint);
        }
        
        private void FireShotgun()
        {
            Vector3 centerTarget = GetTargetPoint();
            
            for (int i = 0; i < weaponData.pelletCount; i++)
            {
                Vector3 targetPoint = GetShotgunTargetPoint();
                CreateBullet(targetPoint);
            }
            
            CreateBulletTrail(firePoint.position, centerTarget);
        }
        
        protected virtual Vector3 GetTargetPoint()
        {
            Vector3 screenCenter = new Vector3(
                Screen.width * GameConstants.Weapons.SCREEN_CENTER_X, 
                Screen.height * GameConstants.Weapons.SCREEN_CENTER_Y, 
                0f);
            Ray cameraRay = _playerCamera.ScreenPointToRay(screenCenter);
            
            Vector3 direction = AddSpread(cameraRay.direction);
            Ray spreadRay = new Ray(cameraRay.origin, direction);
            
            if (Physics.Raycast(spreadRay, out RaycastHit hit, weaponData.range, bulletData.hitLayers))
                return hit.point;
            
            return spreadRay.origin + spreadRay.direction * weaponData.range;
        }
        
        private Vector3 GetShotgunTargetPoint()
        {
            Vector3 screenCenter = new Vector3(
                Screen.width * GameConstants.Weapons.SCREEN_CENTER_X, 
                Screen.height * GameConstants.Weapons.SCREEN_CENTER_Y, 
                0f);
            Ray cameraRay = _playerCamera.ScreenPointToRay(screenCenter);
            
            Vector3 direction = AddShotgunSpread(cameraRay.direction);
            Ray spreadRay = new Ray(cameraRay.origin, direction);
            
            if (Physics.Raycast(spreadRay, out RaycastHit hit, weaponData.range, bulletData.hitLayers))
                return hit.point;
            
            return spreadRay.origin + spreadRay.direction * weaponData.range;
        }
        
        protected virtual Vector3 AddSpread(Vector3 direction)
        {
            Vector3 right = _playerCamera.transform.right;
            Vector3 up = _playerCamera.transform.up;
            
            Vector3 spreadVector = right * Random.Range(-weaponData.spread, weaponData.spread) +
                                 up * Random.Range(-weaponData.spread, weaponData.spread);
            
            return (direction + spreadVector).normalized;
        }
        
        private Vector3 AddShotgunSpread(Vector3 direction)
        {
            Vector3 right = _playerCamera.transform.right;
            Vector3 up = _playerCamera.transform.up;
            
            Vector3 spread = right * Random.Range(-weaponData.pelletSpread, weaponData.pelletSpread) +
                           up * Random.Range(-weaponData.pelletSpread, weaponData.pelletSpread);
            
            return (direction + spread).normalized;
        }
        
        protected virtual void CreateBullet(Vector3 targetPoint)
        {
            if (!bulletPrefab) return;
            
            Vector3 direction = (targetPoint - firePoint.position).normalized;
            
            GameObject bullet;
            if (ObjectPool.Instance?.HasPool(GameConstants.Pools.BULLET) == true)
                bullet = ObjectPool.Instance.SpawnFromPool(GameConstants.Pools.BULLET, firePoint.position, Quaternion.LookRotation(direction));
            else
                bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
            
            bullet?.GetComponent<PhysicsBullet>()?.Initialize(targetPoint, weaponData.bulletSpeed, bulletData, effectData, weaponData.damage);
        }
        
        protected virtual void CreateBulletTrail(Vector3 startPoint, Vector3 endPoint)
        {
            if (!effectData.bulletTrailPrefab) return;
            
            GameObject trail;
            if (ObjectPool.Instance?.HasPool(GameConstants.Pools.BULLET_TRAIL) == true)
                trail = ObjectPool.Instance.SpawnFromPool(GameConstants.Pools.BULLET_TRAIL, Vector3.zero, Quaternion.identity);
            else
                trail = Instantiate(effectData.bulletTrailPrefab);
            
            trail?.GetComponent<BulletTrail>()?.Initialize(startPoint, endPoint);
        }
        
        protected virtual void PlayMuzzleFlash()
        {
            if (!effectData.muzzleFlashPrefab) return;
            
            GameObject flash = Instantiate(effectData.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            flash.transform.SetParent(firePoint);
            
            Destroy(flash, GameConstants.Weapons.MUZZLE_FLASH_LIFETIME);
        }
        
        private void PlayEffects()
        {
            if (audioSource && effectData.fireSound)
                audioSource.PlayOneShot(effectData.fireSound);
            
            PlayMuzzleFlash();
        }
        
        public void StartFiring()
        {
            if (weaponData.isFullAuto)
                _isFullAutoFiring = true;
        }
        
        public void StopFiring()
        {
            _isFullAutoFiring = false;
        }
        
        public virtual void StartReload()
        {
            if (_isReloading || _currentAmmo == weaponData.maxAmmo) return;
            StartCoroutine(ReloadCoroutine());
        }
        
        protected virtual System.Collections.IEnumerator ReloadCoroutine()
        {
            _isReloading = true;
            
            if (audioSource && effectData.reloadSound)
                audioSource.PlayOneShot(effectData.reloadSound);
            
            yield return new WaitForSeconds(weaponData.reloadTime);
            
            _currentAmmo = weaponData.maxAmmo;
            _isReloading = false;
        }
    }
}