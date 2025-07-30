using Constants;
using Managers;
using UnityEngine;

namespace Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsBullet : MonoBehaviour, IPooledObject
    {
        private Vector3 _targetPoint;
        private float _speed;
        private float _damage;
        private BulletData _bulletData;
        private EffectData _effectData;
        
        private Rigidbody _rigidbody;
        private Renderer _renderer;
        
        private int _bounceCount;
        private bool _hasCreatedFirstDecal;
        private bool _hasAppliedDamage;
        private float _spawnTime;
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _renderer = GetComponent<Renderer>();
        }
        
        public void OnObjectSpawn()
        {
            _bounceCount = 0;
            _hasCreatedFirstDecal = false;
            _hasAppliedDamage = false;
            _spawnTime = Time.time;
            
            if (_renderer) _renderer.enabled = true;
            if (_rigidbody) _rigidbody.isKinematic = false;
        }
        
        public void Initialize(Vector3 targetPoint, float speed, BulletData bulletData, EffectData effectData, float damage)
        {
            _targetPoint = targetPoint;
            _speed = speed;
            _bulletData = bulletData;
            _effectData = effectData;
            _damage = damage;
            
            Vector3 direction = (targetPoint - transform.position).normalized;
            _rigidbody.linearVelocity = direction * speed;
        }
        
        private void Update()
        {
            if (Time.time - _spawnTime >= _bulletData.lifetime)
            {
                ReturnToPool();
                return;
            }
            
            if (_hasCreatedFirstDecal) return;
            
            Vector3 velocity = _rigidbody.linearVelocity;
            if (velocity.magnitude > GameConstants.Movement.MOVEMENT_INPUT_THRESHOLD)
            {
                float rayDistance = velocity.magnitude * Time.deltaTime + GameConstants.Bullets.RAYCAST_DISTANCE_OFFSET;
                
                if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, rayDistance, _bulletData.hitLayers))
                {
                    if (Vector3.Distance(transform.position, hit.point) < GameConstants.Bullets.HIT_DISTANCE_THRESHOLD)
                    {
                        ProcessFirstHit(hit.point, hit.normal, hit.collider.gameObject);
                    }
                }
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsValidHit(collision.gameObject)) return;
            
            ContactPoint contact = collision.contacts[0];
            
            if (!_hasCreatedFirstDecal)
            {
                ProcessFirstHit(contact.point, contact.normal, collision.gameObject);
            }
            
            if (_bounceCount < _bulletData.maxBounces)
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
            CreateDecal(hitPoint, hitNormal, surface);
            
            if (!_hasAppliedDamage)
            {
                surface.GetComponent<IDamageable>()?.TakeDamage(_damage);
                _hasAppliedDamage = true;
            }
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
            if (hitObject.layer == LayerMask.NameToLayer(GameConstants.Layers.PLAYER)) return false;
            if (hitObject.name.Contains(GameConstants.Pools.BULLET_DECAL)) return false;
            
            return ((1 << hitObject.layer) & _bulletData.hitLayers) != 0;
        }
        
        private void HandleBounce(Collision collision)
        {
            _bounceCount++;
            
            Vector3 incomingVector = _rigidbody.linearVelocity.normalized;
            Vector3 reflectVector = Vector3.Reflect(incomingVector, collision.contacts[0].normal);
            
            float currentSpeed = _rigidbody.linearVelocity.magnitude;
            _rigidbody.linearVelocity = reflectVector * (currentSpeed * _bulletData.bounceForce);
            _rigidbody.angularVelocity = Random.insideUnitSphere * GameConstants.Bullets.ANGULAR_VELOCITY_MULTIPLIER;
        }
        
        private void CreateImpactEffect(Vector3 position, Vector3 normal)
        {
            if (!_effectData.impactEffectPrefab) return;
            
            Vector3 effectPosition = position + normal * GameConstants.Bullets.IMPACT_EFFECT_OFFSET;
            Quaternion effectRotation = Quaternion.LookRotation(normal);
            
            GameObject effect;
            if (ObjectPool.Instance?.HasPool(GameConstants.Pools.IMPACT_EFFECT) == true)
            {
                effect = ObjectPool.Instance.SpawnFromPool(GameConstants.Pools.IMPACT_EFFECT, effectPosition, effectRotation);
            }
            else
            {
                effect = Instantiate(_effectData.impactEffectPrefab, effectPosition, effectRotation);
            }
            
            if (effect) Destroy(effect, GameConstants.Bullets.IMPACT_EFFECT_LIFETIME);
        }
        
        private void CreateDecal(Vector3 hitPoint, Vector3 hitNormal, GameObject surface)
        {
            if (!_effectData.decalPrefab) return;
            
            Vector3 decalPosition = hitPoint + hitNormal * GameConstants.Bullets.DECAL_POSITION_OFFSET;
            Quaternion decalRotation = Mathf.Abs(hitNormal.y) > 0.7f ? 
                Quaternion.LookRotation(hitNormal) : 
                Quaternion.LookRotation(-hitNormal);
            
            GameObject decal;
            if (ObjectPool.Instance?.HasPool(GameConstants.Pools.BULLET_DECAL) == true)
            {
                decal = ObjectPool.Instance.SpawnFromPool(GameConstants.Pools.BULLET_DECAL, decalPosition, decalRotation);
            }
            else
            {
                decal = Instantiate(_effectData.decalPrefab, decalPosition, decalRotation);
            }
            
            if (decal)
            {
                float decalSize = Random.Range(GameConstants.Bullets.DECAL_SIZE_MIN, GameConstants.Bullets.DECAL_SIZE_MAX);
                decal.transform.localScale = Vector3.one * decalSize;
                decal.transform.Rotate(0, 0, Random.Range(0f, 360f));
                
                var decalManager = decal.GetComponent<DecalManager>();
                if (decalManager && _effectData.decalLifetime > 0)
                {
                    decalManager.Initialize(_effectData.decalLifetime);
                }
                else if (!decalManager && _effectData.decalLifetime > 0)
                {
                    decal.AddComponent<DecalManager>().Initialize(_effectData.decalLifetime);
                }
            }
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