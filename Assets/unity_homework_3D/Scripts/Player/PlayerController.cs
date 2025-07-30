using Constants;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapons;
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

        [Header("Weapon")] 
        public Weapon playerWeapon;

        private CharacterController _controller;
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _verticalRotation;
        private RaycastHit _groundHit;
        private float _currentSlopeAngle;

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

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();

            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
            _inputActions.Player.Look.performed += OnLook;
            _inputActions.Player.Jump.performed += OnJump;
            _inputActions.Player.Jump.canceled += OnJump;
            _inputActions.Player.Sprint.performed += OnSprint;
            _inputActions.Player.Sprint.canceled += OnSprint;
            _inputActions.Player.Attack.performed += OnAttack;
            _inputActions.Player.Reload.performed += OnReload;
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();

            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Look.performed -= OnLook;
            _inputActions.Player.Jump.performed -= OnJump;
            _inputActions.Player.Jump.canceled -= OnJump;
            _inputActions.Player.Sprint.performed -= OnSprint;
            _inputActions.Player.Sprint.canceled -= OnSprint;
            _inputActions.Player.Attack.performed -= OnAttack;
            _inputActions.Player.Reload.performed -= OnReload;
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleMovement();
            HandleMouseLook();
            HandleJump();
            HandleParticleEffects();
        }

        private void HandleGroundCheck()
        {
            Vector3 rayStart = transform.position + Vector3.down * GameConstants.Movement.GROUND_CHECK_OFFSET;
            bool centerRay = Physics.Raycast(rayStart, Vector3.down, out _groundHit, raycastDistance, groundMask);

            bool frontRay = Physics.Raycast(rayStart + transform.forward * GameConstants.Movement.GROUND_CHECK_SIDE_OFFSET, 
                Vector3.down, raycastDistance * GameConstants.Movement.GROUND_CHECK_SIDE_MULTIPLIER, groundMask);
            bool backRay = Physics.Raycast(rayStart - transform.forward * GameConstants.Movement.GROUND_CHECK_SIDE_OFFSET, 
                Vector3.down, raycastDistance * GameConstants.Movement.GROUND_CHECK_SIDE_MULTIPLIER, groundMask);
            bool leftRay = Physics.Raycast(rayStart - transform.right * GameConstants.Movement.GROUND_CHECK_SIDE_OFFSET, 
                Vector3.down, raycastDistance * GameConstants.Movement.GROUND_CHECK_SIDE_MULTIPLIER, groundMask);
            bool rightRay = Physics.Raycast(rayStart + transform.right * GameConstants.Movement.GROUND_CHECK_SIDE_OFFSET, 
                Vector3.down, raycastDistance * GameConstants.Movement.GROUND_CHECK_SIDE_MULTIPLIER, groundMask);

            _isGrounded = centerRay || frontRay || backRay || leftRay || rightRay;

            _currentSlopeAngle = centerRay && _groundHit.normal != Vector3.zero ? 
                Vector3.Angle(_groundHit.normal, Vector3.up) : 0f;
        }

        private void HandleMovement()
        {
            Vector3 direction = transform.right * _movementInput.x + transform.forward * _movementInput.y;
            float currentSpeed = _sprintInput ? runSpeed : walkSpeed;

            if (_isGrounded && _currentSlopeAngle > maxSlopeAngle)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundHit.normal).normalized;
                _controller.Move(slideDirection * (currentSpeed * GameConstants.Movement.SLOPE_SLIDE_MULTIPLIER * Time.deltaTime));
                return;
            }

            if (!_isGrounded) currentSpeed *= airControl;

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
            if (_isGrounded && _velocity.y <= landingVelocityThreshold && 
                Time.time - _lastLandingTime > GameConstants.Movement.LANDING_TIME_THRESHOLD && landingParticles)
            {
                landingParticles.Play();
                _lastLandingTime = Time.time;
            }

            if (sprintWindEffect)
            {
                bool shouldPlayWind = _sprintInput && _isGrounded && _movementInput.magnitude > GameConstants.Movement.MOVEMENT_INPUT_THRESHOLD;
                
                if (shouldPlayWind && !_wasSprintingLastFrame)
                {
                    sprintWindEffect.Play();
                }
                else if (!shouldPlayWind && _wasSprintingLastFrame)
                {
                    sprintWindEffect.Stop();
                }

                _wasSprintingLastFrame = shouldPlayWind;
            }
        }

        private void OnMove(InputAction.CallbackContext context) => _movementInput = context.ReadValue<Vector2>();
        
        private void OnLook(InputAction.CallbackContext context) => _lookInput = context.performed ? context.ReadValue<Vector2>() : Vector2.zero;
        
        private void OnJump(InputAction.CallbackContext context) => _jumpInput = context.ReadValueAsButton();
        
        private void OnSprint(InputAction.CallbackContext context) => _sprintInput = context.ReadValueAsButton();
        
        private void OnAttack(InputAction.CallbackContext context)
        {
            if ((context.started || context.performed) && playerWeapon?.CanFire() == true)
            {
                playerWeapon.Fire();
            }
        }
        
        private void OnReload(InputAction.CallbackContext context)
        {
            if (context.performed && playerWeapon && !playerWeapon.IsReloading)
            {
                playerWeapon.StartReload();
            }
        }
    }
}