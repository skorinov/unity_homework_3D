using Constants;
using UnityEngine;
using Weapons;

namespace Collectibles
{
    /// <summary>
    /// Makes weapon collectible with visual feedback
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Weapon))]
    public class CollectibleWeapon : MonoBehaviour
    {
        private static readonly Color HighlightColor = Color.yellow;
        
        private Vector3 _startPosition;
        private bool _isAttached;
        private bool _isHighlighted;
        private bool _isFloating;
        private bool _wasPickedUp;
        
        private Rigidbody _rb;
        private Collider _col;
        private Renderer[] _renderers;
        private Material[] _originalMaterials;
        private Material[] _highlightMaterials;
        private Weapon _weaponComponent;
        
        // Properties
        public bool IsAttached => _isAttached;
        public bool IsHighlighted => _isHighlighted;
        public Weapon WeaponComponent => _weaponComponent;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _weaponComponent = GetComponent<Weapon>();
            
            SetupHighlightMaterials();
        }
        
        private void Start()
        {
            _startPosition = transform.position;
            
            // Start floating animation if never picked up
            if (!_wasPickedUp)
                Invoke(nameof(StartFloating), 1f);
            
            // Disable weapon component when not held
            if (_weaponComponent)
                _weaponComponent.enabled = false;
        }
        
        private void Update()
        {
            if (_isFloating && !_isAttached)
                HandleFloatingAnimation();
        }
        
        private void HandleFloatingAnimation()
        {
            if (!_rb.isKinematic) return;
            
            float newY = _startPosition.y + Mathf.Sin(Time.time * GameConstants.Collectibles.BOB_SPEED) * GameConstants.Collectibles.BOB_HEIGHT;
            transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
        }
        
        private void StartFloating()
        {
            if (_isAttached || _wasPickedUp) return;
            
            _rb.isKinematic = true;
            _rb.detectCollisions = true;
            
            // Elevate and start floating
            _startPosition = transform.position + Vector3.up * GameConstants.Collectibles.HEIGHT_OFFSET;
            transform.position = _startPosition;
            _isFloating = true;
        }
        
        private void SetupHighlightMaterials()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_renderers.Length];
            _highlightMaterials = new Material[_renderers.Length];
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (!_renderers[i]) continue;
                
                _originalMaterials[i] = _renderers[i].material;
                
                // Create highlight material
                _highlightMaterials[i] = new Material(_originalMaterials[i]);
                _highlightMaterials[i].EnableKeyword("_EMISSION");
                _highlightMaterials[i].SetColor("_EmissionColor", HighlightColor * GameConstants.Collectibles.HIGHLIGHT_INTENSITY);
            }
        }
        
        public void Attach(Transform holder)
        {
            if (_isAttached) return;
            
            _isAttached = true;
            _isFloating = false;
            _wasPickedUp = true;
            
            // Stop physics
            _rb.isKinematic = true;
            _col.isTrigger = true;
            
            // Remove highlight
            SetHighlight(false);
            
            // Attach to holder
            transform.SetParent(holder);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            // Enable weapon component
            if (_weaponComponent)
                _weaponComponent.enabled = true;
        }
        
        public void Detach()
        {
            if (!_isAttached) return;
            
            // Store world transform
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;
            
            // Detach from parent
            transform.SetParent(null);
            transform.position = worldPos;
            transform.rotation = worldRot;
            
            // Enable physics
            _rb.isKinematic = false;
            _col.isTrigger = false;
            
            // Add drop impulse
            Vector3 dropForce = transform.forward * 3f + Vector3.up * 1f;
            _rb.AddForce(dropForce, ForceMode.Impulse);
            
            _isAttached = false;
            
            // Disable weapon component
            if (_weaponComponent)
                _weaponComponent.enabled = false;
        }
        
        public void SetHighlight(bool highlight)
        {
            if (_isAttached || _isHighlighted == highlight) return;
            
            _isHighlighted = highlight;
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] && i < _originalMaterials.Length && i < _highlightMaterials.Length)
                {
                    _renderers[i].material = highlight ? _highlightMaterials[i] : _originalMaterials[i];
                }
            }
        }
        
        private void OnDestroy()
        {
            // Clean up highlight materials
            if (_highlightMaterials != null)
            {
                for (int i = 0; i < _highlightMaterials.Length; i++)
                {
                    if (_highlightMaterials[i])
                        Destroy(_highlightMaterials[i]);
                }
            }
        }
    }
}