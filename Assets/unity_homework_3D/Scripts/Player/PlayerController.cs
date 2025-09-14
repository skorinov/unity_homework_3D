using Constants;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = Core.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")] 
        public float walkSpeed = 10f;
        public float runSpeed = 20f;
        public float jumpHeight = 2f;
        public float gravity = -20f;
        public float airControl = 0.3f;

        [Header("Camera Settings")] 
        public Transform playerCamera;
        public float mouseSensitivity = 20f;
        public float maxLookAngle = 80f;

        [Header("Ground Check")] 
        public float raycastDistance = 1.2f;
        public LayerMask groundMask;
        public float maxSlopeAngle = 45f;

        [Header("Particle Effects")] 
        public ParticleSystem landingParticles;
        public ParticleSystem sprintWindEffect;
        public float landingVelocityThreshold = -5f;

        [Header("Weapon System")] 
        public WeaponOwner weaponOwner;

        private CharacterController _controller;
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _verticalRotation;
        private RaycastHit _groundHit;
        private float _currentSlopeAngle;

        // Input state
        private Vector2 _movementInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _sprintInput;
        private bool _wasSprintingLastFrame;
        private float _lastLandingTime;

        private InputSystem _inputActions;

        private void Awake()
        {
            _inputActions = new InputSystem();
            _controller = GetComponent<CharacterController>();

            if (!weaponOwner)
                weaponOwner = GetComponent<WeaponOwner>();

            SetCursorState(true);
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            SubscribeToInputEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromInputEvents();
            _inputActions.Player.Disable();
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleMovement();
            HandleMouseLook();
            HandleJump();
            HandleParticleEffects();
        }

        private void SubscribeToInputEvents()
        {
            var player = _inputActions.Player;
            
            player.Move.performed += OnMove;
            player.Move.canceled += OnMove;
            player.Look.performed += OnLook;
            player.Jump.performed += OnJump;
            player.Jump.canceled += OnJump;
            player.Sprint.performed += OnSprint;
            player.Sprint.canceled += OnSprint;
            player.Attack.started += OnAttackStarted;
            player.Attack.canceled += OnAttackCanceled;
            player.Reload.performed += OnReload;
            player.Interact.performed += OnInteract;
        }

        private void UnsubscribeFromInputEvents()
        {
            var player = _inputActions.Player;
            
            player.Move.performed -= OnMove;
            player.Move.canceled -= OnMove;
            player.Look.performed -= OnLook;
            player.Jump.performed -= OnJump;
            player.Jump.canceled -= OnJump;
            player.Sprint.performed -= OnSprint;
            player.Sprint.canceled -= OnSprint;
            player.Attack.started -= OnAttackStarted;
            player.Attack.canceled -= OnAttackCanceled;
            player.Reload.performed -= OnReload;
            player.Interact.performed -= OnInteract;
        }

        private void HandleGroundCheck()
        {
            Vector3 rayStart = transform.position + Vector3.down * GameConstants.Movement.GROUND_CHECK_OFFSET;
            
            bool centerRay = Physics.Raycast(rayStart, Vector3.down, out _groundHit, raycastDistance, groundMask);
            bool sideRaysHit = CheckSideRays(rayStart);
            
            _isGrounded = centerRay || sideRaysHit;
            _currentSlopeAngle = centerRay && _groundHit.normal != Vector3.zero ? 
                Vector3.Angle(_groundHit.normal, Vector3.up) : 0f;
        }

        private bool CheckSideRays(Vector3 rayStart)
        {
            float sideOffset = GameConstants.Movement.GROUND_CHECK_SIDE_OFFSET;
            float sideDistance = raycastDistance * GameConstants.Movement.GROUND_CHECK_SIDE_MULTIPLIER;
            
            return Physics.Raycast(rayStart + transform.forward * sideOffset, Vector3.down, sideDistance, groundMask) ||
                   Physics.Raycast(rayStart - transform.forward * sideOffset, Vector3.down, sideDistance, groundMask) ||
                   Physics.Raycast(rayStart - transform.right * sideOffset, Vector3.down, sideDistance, groundMask) ||
                   Physics.Raycast(rayStart + transform.right * sideOffset, Vector3.down, sideDistance, groundMask);
        }

        private void HandleMovement()
        {
            Vector3 direction = transform.right * _movementInput.x + transform.forward * _movementInput.y;
            float currentSpeed = _sprintInput ? runSpeed : walkSpeed;

            // Handle slope sliding
            if (_isGrounded && _currentSlopeAngle > maxSlopeAngle)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundHit.normal).normalized;
                _controller.Move(slideDirection * (currentSpeed * GameConstants.Movement.SLOPE_SLIDE_MULTIPLIER * Time.deltaTime));
                return;
            }

            // Apply air control
            if (!_isGrounded) 
                currentSpeed *= airControl;

            _controller.Move(direction * (currentSpeed * Time.deltaTime));
        }

        private void HandleMouseLook()
        {
            if (_lookInput.sqrMagnitude < GameConstants.Movement.LOOK_INPUT_THRESHOLD) return;

            float mouseX = _lookInput.x * mouseSensitivity * GameConstants.Movement.MOUSE_SENSITIVITY_MULTIPLIER;
            float mouseY = _lookInput.y * mouseSensitivity * GameConstants.Movement.MOUSE_SENSITIVITY_MULTIPLIER;

            transform.Rotate(0, mouseX, 0);

            _verticalRotation = Mathf.Clamp(_verticalRotation - mouseY, -maxLookAngle, maxLookAngle);
            playerCamera.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);

            _lookInput = Vector2.zero;
        }

        private void HandleJump()
        {
            if (_jumpInput && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpInput = false;
            }

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = GameConstants.Movement.VELOCITY_RESET;
            }
        }

        private void HandleParticleEffects()
        {
            // Landing particles
            if (_isGrounded && _velocity.y <= landingVelocityThreshold && 
                Time.time - _lastLandingTime > GameConstants.Movement.LANDING_TIME_THRESHOLD && landingParticles)
            {
                landingParticles.Play();
                _lastLandingTime = Time.time;
            }

            // Sprint wind effect
            if (sprintWindEffect)
            {
                bool shouldPlayWind = _sprintInput && _isGrounded && 
                    _movementInput.magnitude > GameConstants.Movement.MOVEMENT_INPUT_THRESHOLD;
                
                if (shouldPlayWind != _wasSprintingLastFrame)
                {
                    if (shouldPlayWind)
                        sprintWindEffect.Play();
                    else
                        sprintWindEffect.Stop();
                }

                _wasSprintingLastFrame = shouldPlayWind;
            }
        }

        private void SetCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // Input event handlers
        private void OnMove(InputAction.CallbackContext context) => _movementInput = context.ReadValue<Vector2>();
        
        private void OnLook(InputAction.CallbackContext context) => _lookInput = context.performed ? context.ReadValue<Vector2>() : Vector2.zero;
        
        private void OnJump(InputAction.CallbackContext context) => _jumpInput = context.ReadValueAsButton();
        
        private void OnSprint(InputAction.CallbackContext context) => _sprintInput = context.ReadValueAsButton();
        
        private void OnAttackStarted(InputAction.CallbackContext context)
        {
            if (!weaponOwner?.HasWeapon == true) return;
            
            if (weaponOwner.CanFire())
                weaponOwner.Fire();
            
            if (weaponOwner.CurrentWeapon?.IsFullAuto == true)
                weaponOwner.StartFiring();
        }
        
        private void OnAttackCanceled(InputAction.CallbackContext context)
        {
            if (weaponOwner?.HasWeapon == true)
                weaponOwner.StopFiring();
        }
        
        private void OnReload(InputAction.CallbackContext context)
        {
            if (weaponOwner?.HasWeapon == true)
                weaponOwner.Reload();
        }
        
        private void OnInteract(InputAction.CallbackContext context)
        {
            weaponOwner?.TryPickupWeapon();
        }
    }
}