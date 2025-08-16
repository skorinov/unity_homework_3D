using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Weapon configuration data
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapons/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponName = "Weapon";
        public WeaponType weaponType = WeaponType.Rifle;
        public Sprite weaponIcon;
        
        [Header("Combat Stats")]
        public float damage = 25f;
        public float fireRate = 600f;
        public float range = 100f;
        public int maxAmmo = 30;
        public float reloadTime = 2f;
        public float spread = 0.02f;
        public float bulletSpeed = 50f;
        
        [Header("Bullet Behavior")]
        public int maxBounces = 2;
        public float bounceForce = 0.5f;
        public LayerMask hitLayers = -1;
        
        [Header("Special Features")]
        public bool isFullAuto = false;
        public bool isShotgun = false;
        public int pelletCount = 8;
        public float pelletSpread = 0.15f;
        
        [Header("Audio & Effects")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public GameObject muzzleFlashPrefab;
        public GameObject impactEffectPrefab;
        public GameObject bulletTrailPrefab;
    }
    
    public enum WeaponType
    {
        Pistol,
        Rifle,
        Shotgun,
        Sniper,
        SMG
    }
}