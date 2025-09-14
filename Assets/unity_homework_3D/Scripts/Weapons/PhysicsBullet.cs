using Constants;
using Managers;
using UnityEngine;

namespace Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsBullet : MonoBehaviour, IPooledObject
    {
        private float _damage;
        private bool _isPlayerBullet;
        
        private Rigidbody _rigidbody;
        private Renderer _renderer;
        
        private int _bounceCount;
        private bool _hasCreatedFirstDecal;
        private bool _hasAppliedDamage;
        private float _spawnTime;
        
        // Cached layer values for performance
        private int _playerLayer;
        private int _enemyLayer;
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _renderer = GetComponent<Renderer>();
            
            // Cache layer values
            _playerLayer = LayerMask.NameToLayer(GameConstants.Layers.PLAYER);
            _enemyLayer = LayerMask.NameToLayer(GameConstants.Layers.ENEMY);
        }
        
        public void OnObjectSpawn()
        {
            _bounceCount = 0;
            _hasCreatedFirstDecal = false;
            _hasAppliedDamage = false;
            _spawnTime = Time.time;
            _isPlayerBullet = false;
            
            if (_renderer) _renderer.enabled = true;
            if (_rigidbody) _rigidbody.isKinematic = false;
        }
        
        public void Initialize(Vector3 targetPoint, float speed, float damage, bool isPlayerBullet)
        {
            _damage = damage;
            _isPlayerBullet = isPlayerBullet;
            
            Vector3 direction = (targetPoint - transform.position).normalized;
            _rigidbody.linearVelocity = direction * speed;
        }
        
        private void Update()
        {
            // Check lifetime
            if (Time.time - _spawnTime >= GameConstants.Bullets.DEFAULT_LIFETIME)
            {
                ReturnToPool();
                return;
            }
            
            // Early raycast for first hit detection
            if (!_hasCreatedFirstDecal && _rigidbody.linearVelocity.sqrMagnitude > GameConstants.Movement.MOVEMENT_INPUT_THRESHOLD)
            {
                CheckForHit();
            }
        }
        
        private void CheckForHit()
        {
            Vector3 velocity = _rigidbody.linearVelocity;
            float rayDistance = velocity.magnitude * Time.deltaTime + GameConstants.Bullets.RAYCAST_DISTANCE_OFFSET;
            
            if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, rayDistance, GameConstants.Bullets.DEFAULT_HIT_LAYERS))
            {
                float hitDistance = Vector3.Distance(transform.position, hit.point);
                if (hitDistance < GameConstants.Bullets.HIT_DISTANCE_THRESHOLD)
                {
                    ProcessFirstHit(hit.point, hit.normal, hit.collider.gameObject);
                }
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            GameObject hitObject = collision.gameObject;
            int hitLayer = hitObject.layer;
            
            // Check friendly fire
            if ((_isPlayerBullet && hitLayer == _playerLayer) || (!_isPlayerBullet && hitLayer == _enemyLayer))
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider);
                return;
            }
            
            if (!IsValidHit(hitObject)) return;
            
            ContactPoint contact = collision.contacts[0];
            
            if (!_hasCreatedFirstDecal)
            {
                ProcessFirstHit(contact.point, contact.normal, hitObject);
            }
            
            if (_bounceCount < GameConstants.Bullets.DEFAULT_MAX_BOUNCES)
            {
                HandleBounce(collision);
            }
            else
            {
                StopBullet();
            }
        }
        
        private void ProcessFirstHit(Vector3 hitPoint, Vector3 hitNormal, GameObject surface)
        {
            if (_hasCreatedFirstDecal) return;
            _hasCreatedFirstDecal = true;
            
            CreateImpactEffect(hitPoint, hitNormal);
            
            if (ShouldCreateDecal(surface))
            {
                CreateDecal(hitPoint, hitNormal);
            }
            
            if (!_hasAppliedDamage)
            {
                ApplyDamage(surface);
                _hasAppliedDamage = true;
            }
        }
        
        private bool ShouldCreateDecal(GameObject surface)
        {
            int surfaceLayer = surface.layer;
            
            // No decals on living entities
            if ((GameConstants.Layers.LIVING_ENTITIES_MASK & (1 << surfaceLayer)) != 0)
                return false;
            
            // Check if surface supports decals
            if ((GameConstants.Layers.DECAL_SURFACES_MASK & (1 << surfaceLayer)) != 0)
                return true;
            
            // Special case for environment layer
            if (surfaceLayer == LayerMask.NameToLayer(GameConstants.Layers.ENVIRONMENT))
            {
                return surface.GetComponent<IDamageable>() == null && 
                       surface.GetComponentInParent<IDamageable>() == null;
            }
            
            return false;
        }
        
        private void ApplyDamage(GameObject target)
        {
            int targetLayer = target.layer;
            
            // Check friendly fire
            if ((_isPlayerBullet && targetLayer == _playerLayer) || (!_isPlayerBullet && targetLayer == _enemyLayer))
                return;
            
            // Try to find IDamageable component
            IDamageable damageable = target.GetComponent<IDamageable>() ?? 
                                   target.GetComponentInParent<IDamageable>() ?? 
                                   target.GetComponentInChildren<IDamageable>();
            
            damageable?.TakeDamage(_damage);
        }
        
        private void StopBullet()
        {
            if (_rigidbody && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
            
            if (_renderer) _renderer.enabled = false;
            
            Invoke(nameof(ReturnToPool), GameConstants.Bullets.STOP_DELAY);
        }
        
        private bool IsValidHit(GameObject hitObject)
        {
            int hitLayer = hitObject.layer;
            
            // Ignore bullet decals and other bullets
            if (hitObject.name.Contains(GameConstants.Pools.BULLET_DECAL) || 
                hitObject.GetComponent<PhysicsBullet>())
                return false;
            
            return ((1 << hitLayer) & GameConstants.Bullets.DEFAULT_HIT_LAYERS) != 0;
        }
        
        private void HandleBounce(Collision collision)
        {
            _bounceCount++;
            
            Vector3 incomingVector = _rigidbody.linearVelocity.normalized;
            Vector3 reflectVector = Vector3.Reflect(incomingVector, collision.contacts[0].normal);
            
            float currentSpeed = _rigidbody.linearVelocity.magnitude;
            _rigidbody.linearVelocity = reflectVector * (currentSpeed * GameConstants.Bullets.DEFAULT_BOUNCE_FORCE);
            _rigidbody.angularVelocity = Random.insideUnitSphere * GameConstants.Bullets.ANGULAR_VELOCITY_MULTIPLIER;
        }
        
        private void CreateImpactEffect(Vector3 position, Vector3 normal)
        {
            Vector3 effectPosition = position + normal * GameConstants.Bullets.IMPACT_EFFECT_OFFSET;
            Quaternion effectRotation = Quaternion.LookRotation(normal);
            
            ObjectPool.Instance?.SpawnFromPoolTimed(
                GameConstants.Pools.IMPACT_EFFECT, 
                effectPosition, 
                effectRotation,
                GameConstants.Bullets.IMPACT_EFFECT_LIFETIME
            );
        }
        
        private void CreateDecal(Vector3 hitPoint, Vector3 hitNormal)
        {
            Vector3 decalPosition = hitPoint + hitNormal * GameConstants.Bullets.DECAL_POSITION_OFFSET;
            Quaternion decalRotation = Mathf.Abs(hitNormal.y) > 0.7f 
                ? Quaternion.LookRotation(hitNormal) 
                : Quaternion.LookRotation(-hitNormal);
            
            ObjectPool.Instance?.SpawnFromPool<DecalManager>(
                GameConstants.Pools.BULLET_DECAL, 
                decalPosition, 
                decalRotation,
                decal => {
                    float decalSize = Random.Range(GameConstants.Bullets.DECAL_SIZE_MIN, GameConstants.Bullets.DECAL_SIZE_MAX);
                    decal.transform.localScale = Vector3.one * decalSize;
                    decal.transform.Rotate(0, 0, Random.Range(0f, 360f));
                    decal.Initialize(GameConstants.Decals.DEFAULT_LIFETIME);
                }
            );
        }
        
        private void ReturnToPool()
        {
            if (ObjectPool.Instance?.HasPool(GameConstants.Pools.BULLET) == true)
            {
                ObjectPool.Instance.ReturnToPool(GameConstants.Pools.BULLET, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}