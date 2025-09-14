using Constants;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Manages decal lifecycle with fade out and pool return
    /// </summary>
    public class DecalManager : MonoBehaviour, IPooledObject
    {
        private float _lifetime;
        private float _fadeTime = GameConstants.Decals.DEFAULT_FADE_TIME;
        private Material _material;
        private Color _originalColor;
        private Renderer _renderer;
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            SetupMaterial();
        }
        
        public void OnObjectSpawn()
        {
            if (_material && _renderer)
            {
                _material.color = _originalColor;
                _renderer.enabled = true;
            }
            
            StopAllCoroutines();
        }
        
        public void Initialize(float lifetime)
        {
            _lifetime = lifetime;
            StartCoroutine(DestroySequence());
        }
        
        private void SetupMaterial()
        {
            if (_renderer?.material)
            {
                _material = new Material(_renderer.material);
                _renderer.material = _material;
                _originalColor = _material.color;
            }
        }
        
        private System.Collections.IEnumerator DestroySequence()
        {
            yield return new WaitForSeconds(_lifetime - _fadeTime);
            
            if (_material)
            {
                yield return StartCoroutine(FadeOut());
            }
            else
            {
                yield return new WaitForSeconds(_fadeTime);
            }
            
            ReturnToPool();
        }
        
        private System.Collections.IEnumerator FadeOut()
        {
            float fadeTimer = 0f;
            
            while (fadeTimer < _fadeTime)
            {
                fadeTimer += Time.deltaTime;
                float alpha = Mathf.Lerp(_originalColor.a, 0f, fadeTimer / _fadeTime);
                
                _material.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
                
                yield return null;
            }
        }
        
        private void ReturnToPool()
        {
            if (ObjectPool.Instance?.HasPool(GameConstants.Pools.BULLET_DECAL) == true)
            {
                ObjectPool.Instance.ReturnToPool(GameConstants.Pools.BULLET_DECAL, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            if (_material) 
                Destroy(_material);
        }
    }
}