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
    
        // Private variables
        private CharacterController _controller;
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _verticalRotation;
        private RaycastHit _groundHit;
        private float _currentSlopeAngle;
    
        // Input variables
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
        }
    
        private void Update()
        {
            HandleGroundCheck();
            HandleMovement();
            HandleMouseLook();
            
            HandleLandingParticles();
            
            HandleJump();
            HandleSprintWindEffect();
        }
    
        private void HandleGroundCheck()
        {
            Vector3 rayStart = transform.position + Vector3.down * 0.1f;
            bool centerRay = Physics.Raycast(rayStart, Vector3.down, out _groundHit, raycastDistance, groundMask);
            
            // Multiple rays for stability
            bool frontRay = Physics.Raycast(rayStart + transform.forward * 0.3f, Vector3.down, raycastDistance * 0.8f, groundMask);
            bool backRay = Physics.Raycast(rayStart - transform.forward * 0.3f, Vector3.down, raycastDistance * 0.8f, groundMask);
            bool leftRay = Physics.Raycast(rayStart - transform.right * 0.3f, Vector3.down, raycastDistance * 0.8f, groundMask);
            bool rightRay = Physics.Raycast(rayStart + transform.right * 0.3f, Vector3.down, raycastDistance * 0.8f, groundMask);
            
            _isGrounded = centerRay || frontRay || backRay || leftRay || rightRay;
            
            if (centerRay && _groundHit.normal != Vector3.zero)
            {
                _currentSlopeAngle = Vector3.Angle(_groundHit.normal, Vector3.up);
            }
            else
            {
                _currentSlopeAngle = 0f;
            }
        }
    
        private void HandleMovement()
        {
            Vector3 direction = transform.right * _movementInput.x + transform.forward * _movementInput.y;
            float currentSpeed = _sprintInput ? runSpeed : walkSpeed;
            
            // Handle steep slopes
            if (_isGrounded && _currentSlopeAngle > maxSlopeAngle)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundHit.normal).normalized;
                _controller.Move(slideDirection * (currentSpeed * 0.5f * Time.deltaTime));
                return;
            }
            
            // Air control
            if (!_isGrounded)
            {
                currentSpeed *= airControl;
            }
            
            _controller.Move(direction * (currentSpeed * Time.deltaTime));
        }
    
        private void HandleMouseLook()
        {
            // Ignore very small values to prevent drift
            if (Mathf.Abs(_lookInput.x) < 0.01f && Mathf.Abs(_lookInput.y) < 0.01f)
            {
                return;
            }
            
            float mouseX = _lookInput.x * mouseSensitivity * 0.01f;
            float mouseY = _lookInput.y * mouseSensitivity * 0.01f;
            
            // Horizontal rotation (player)
            transform.Rotate(0, mouseX, 0);
            
            // Vertical rotation (camera only)
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -maxLookAngle, maxLookAngle);
            playerCamera.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            
            // Clear input to prevent drift
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
            
            // Reset vertical velocity
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }
    
        private void HandleLandingParticles()
        {
            bool shouldPlayParticles = _isGrounded && 
                                     _velocity.y <= landingVelocityThreshold && 
                                     Time.time - _lastLandingTime > 0.3f;
            
            if (shouldPlayParticles && landingParticles)
            {
                landingParticles.Play();
                _lastLandingTime = Time.time;
            }
        }
        
        private void HandleSprintWindEffect()
        {
            if(!sprintWindEffect) return;
            // Enable wind effect when sprinting and moving
            if (_sprintInput && _isGrounded && _movementInput.magnitude > 0.1f)
            {
                if (!_wasSprintingLastFrame)
                {
                    sprintWindEffect.Play();
                }
                _wasSprintingLastFrame = true;
            }
            else
            {
                // Disable wind effect
                if (_wasSprintingLastFrame)
                {
                    sprintWindEffect.Stop();
                }
                _wasSprintingLastFrame = false;
            }
        }
    
        // Input callbacks
        private void OnMove(InputAction.CallbackContext context)
        {
            _movementInput = context.ReadValue<Vector2>();
        }
    
        private void OnLook(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _lookInput = context.ReadValue<Vector2>();
            }
            else if (context.canceled)
            {
                _lookInput = Vector2.zero;
            }
        }
    
        private void OnJump(InputAction.CallbackContext context)
        {
            _jumpInput = context.ReadValueAsButton();
        }
    
        private void OnSprint(InputAction.CallbackContext context)
        {
            _sprintInput = context.ReadValueAsButton();
        }
    }
}