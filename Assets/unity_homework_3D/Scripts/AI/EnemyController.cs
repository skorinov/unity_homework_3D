using Constants;
using UnityEngine;
using UnityEngine.AI;
using Weapons;

namespace AI
{
    /// <summary>
    /// AI enemy controller with patrol, chase, and combat behaviors
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Combat Settings")]
        [SerializeField] private float detectionRange = 50f;
        [SerializeField] private float attackRange = GameConstants.AI.DEFAULT_ATTACK_RANGE;
        [SerializeField] private float loseTargetDistance = 25f;
        [SerializeField] private float maxHealth = GameConstants.Health.DEFAULT_ENEMY_HEALTH;
        [SerializeField] private float aimTime = 0.5f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float detectionAngle = 180f;

        [Header("Player Detection")]
        [SerializeField] private string playerTag = GameConstants.Layers.PLAYER;
        [SerializeField] private float playerHeight = 1.8f;
        [SerializeField] private float memoryTime = 8f;

        [Header("Patrol Settings")]
        [SerializeField] private PatrolType patrolType = PatrolType.Random;
        [SerializeField] private float baseWaitTime = GameConstants.AI.DEFAULT_PATROL_WAIT_TIME;
        [SerializeField] private float waitTimeVariation = 1f;
        [SerializeField] private float patrolSpeed = GameConstants.AI.DEFAULT_AGENT_SPEED;
        [SerializeField] private float combatSpeed = 4f;
        [SerializeField] private float minPatrolDistance = 3f;

        [Header("Movement")]
        [SerializeField] private float stopDistance = GameConstants.AI.DEFAULT_STOPPING_DISTANCE;
        [SerializeField] private float acceleration = GameConstants.AI.DEFAULT_AGENT_ACCELERATION;
        [SerializeField] private float angularSpeed = GameConstants.AI.DEFAULT_AGENT_ANGULAR_SPEED;

        [Header("Components")]
        [SerializeField] private EnemyWeaponController weaponController;
        [SerializeField] private Transform aimTarget;

        private NavMeshAgent _agent;
        private Animator _animator;
        private Transform _player;
        private Camera _playerCamera;
        private EnemyState _currentState = EnemyState.Patrolling;

        // Patrol state
        private Transform[] _patrolPoints;
        private int _currentPatrolIndex = 0;
        private bool _isForward = true;
        private float _waitTimer = 0f;
        private float _currentWaitTime;
        private bool _isWaiting = false;

        // Combat state
        private float _currentHealth;
        private float _lastSeenPlayerTime;
        private Vector3 _lastKnownPlayerPosition;
        private float _aimTimer = 0f;
        private bool _isAiming = false;

        // Cached animation hashes for performance
        private static readonly int IsCombat = Animator.StringToHash("IsCombat");
        private static readonly int IsShooting = Animator.StringToHash("IsShooting");
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
        private static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int Speed = Animator.StringToHash("Speed");

        // Properties
        public float Health => _currentHealth;
        public bool IsAlive => _currentHealth > 0;
        public EnemyState CurrentState => _currentState;
        public Transform AimTarget => aimTarget ? aimTarget : transform;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _currentHealth = maxHealth;

            if (!weaponController)
                weaponController = GetComponent<EnemyWeaponController>();
        }

        private void Start()
        {
            FindPlayer();
            SetupAgent();
            GenerateRandomWaitTime();
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (!_player)
                FindPlayer();

            if (_agent.enabled && !_agent.isOnNavMesh)
                RecoverAgent();

            CheckForPlayer();
            UpdateStateMachine();
            UpdateAnimations();
            
            // Debug info
            if (weaponController)
            {
                Debug.DrawLine(transform.position, transform.position + transform.forward * 2f, Color.blue, 0.1f);
                if (_player)
                    Debug.DrawLine(transform.position, _player.position, Color.green, 0.1f);
            }
        }

        private void FindPlayer()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject)
            {
                _player = playerObject.transform;
                // Set target for weapon controller when player is found
                if (weaponController)
                    weaponController.SetTarget(_player);
            }

            _playerCamera = Camera.main;
            if (!_playerCamera)
                _playerCamera = FindFirstObjectByType<Camera>();

            if (!_player)
            {
                var playerController = FindFirstObjectByType<Player.PlayerController>();
                if (playerController)
                {
                    _player = playerController.transform;
                    // Set target for weapon controller
                    if (weaponController)
                        weaponController.SetTarget(_player);
                }
            }
        }

        private void CheckForPlayer()
        {
            if (!_player) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distanceToPlayer <= detectionRange)
            {
                if (IsPlayerInFieldOfView() && HasLineOfSight())
                {
                    PlayerDetected(distanceToPlayer);
                }
            }

            // Check if player is lost
            if (_currentState != EnemyState.Patrolling &&
                Time.time - _lastSeenPlayerTime > memoryTime)
            {
                ChangeState(EnemyState.Patrolling);
            }
        }

        private bool IsPlayerInFieldOfView()
        {
            if (_currentState != EnemyState.Patrolling) return true; // Always track if already engaged

            Vector3 directionToPlayer = GetPlayerCenter() - transform.position;
            directionToPlayer.y = 0;
            directionToPlayer = directionToPlayer.normalized;

            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            return angleToPlayer <= detectionAngle * 0.5f;
        }

        private void PlayerDetected(float distance)
        {
            _lastSeenPlayerTime = Time.time;
            _lastKnownPlayerPosition = GetPlayerCenter();

            if (distance <= attackRange && _currentState != EnemyState.Attacking)
                ChangeState(EnemyState.Attacking);
            else if (_currentState == EnemyState.Patrolling || _currentState == EnemyState.Searching)
                ChangeState(EnemyState.Chasing);
        }

        private bool HasLineOfSight()
        {
            if (!_player) return false;

            Vector3 startPos = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = GetPlayerHeadPosition();
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);

            // Ignore own colliders when raycasting
            Collider[] ownColliders = GetComponentsInChildren<Collider>();
            foreach (var col in ownColliders)
                col.enabled = false;

            bool hasLOS = true;
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, distance))
            {
                hasLOS = hit.transform == _player ||
                        hit.transform.IsChildOf(_player) ||
                        hit.transform.CompareTag(playerTag);
            }

            // Re-enable own colliders
            foreach (var col in ownColliders)
                col.enabled = true;

            return hasLOS;
        }

        private void UpdateStateMachine()
        {
            switch (_currentState)
            {
                case EnemyState.Patrolling:
                    HandlePatrolling();
                    break;
                case EnemyState.Chasing:
                    HandleChasing();
                    break;
                case EnemyState.Attacking:
                    HandleAttacking();
                    break;
                case EnemyState.Searching:
                    HandleSearching();
                    break;
            }
        }

        private void HandlePatrolling()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    MoveToNextPatrolPoint();
                }
                return;
            }

            // Check if reached current patrol point
            if (!_agent.pathPending && _agent.remainingDistance < minPatrolDistance)
            {
                StartWaiting();
            }
        }

        private void StartWaiting()
        {
            _isWaiting = true;
            _waitTimer = _currentWaitTime;
            _agent.isStopped = true;
            GenerateRandomWaitTime();
        }

        private void GenerateRandomWaitTime()
        {
            _currentWaitTime = baseWaitTime + Random.Range(-waitTimeVariation, waitTimeVariation);
            _currentWaitTime = Mathf.Max(0.5f, _currentWaitTime);
        }

        private void MoveToNextPatrolPoint()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;

            int nextIndex = GetNextPatrolIndex();

            if (nextIndex >= 0 && nextIndex < _patrolPoints.Length && _patrolPoints[nextIndex])
            {
                _currentPatrolIndex = nextIndex;
                _agent.isStopped = false;
                SetDestination(_patrolPoints[_currentPatrolIndex].position);
            }
        }

        private int GetNextPatrolIndex()
        {
            switch (patrolType)
            {
                case PatrolType.Loop:
                    return (_currentPatrolIndex + 1) % _patrolPoints.Length;

                case PatrolType.PingPong:
                    if (_isForward)
                    {
                        if (_currentPatrolIndex >= _patrolPoints.Length - 1)
                        {
                            _isForward = false;
                            return Mathf.Max(0, _currentPatrolIndex - 1);
                        }
                        return _currentPatrolIndex + 1;
                    }
                    else
                    {
                        if (_currentPatrolIndex <= 0)
                        {
                            _isForward = true;
                            return Mathf.Min(_patrolPoints.Length - 1, _currentPatrolIndex + 1);
                        }
                        return _currentPatrolIndex - 1;
                    }

                case PatrolType.Random:
                    int newIndex;
                    int attempts = 0;
                    do
                    {
                        newIndex = Random.Range(0, _patrolPoints.Length);
                        attempts++;
                    } while (newIndex == _currentPatrolIndex && _patrolPoints.Length > 1 && attempts < 10);
                    return newIndex;

                default:
                    return (_currentPatrolIndex + 1) % _patrolPoints.Length;
            }
        }

        private void HandleChasing()
        {
            if (!_player)
            {
                ChangeState(EnemyState.Patrolling);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distanceToPlayer > loseTargetDistance && Time.time - _lastSeenPlayerTime > 3f)
            {
                ChangeState(EnemyState.Searching);
                return;
            }

            if (distanceToPlayer <= attackRange && HasLineOfSight())
            {
                ChangeState(EnemyState.Attacking);
                return;
            }

            SetDestination(_lastKnownPlayerPosition);

            if (HasLineOfSight())
            {
                LookAtPlayer();
                _lastSeenPlayerTime = Time.time;
                _lastKnownPlayerPosition = GetPlayerCenter();
            }
        }

        private void HandleAttacking()
        {
            if (!_player)
            {
                ChangeState(EnemyState.Patrolling);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distanceToPlayer > attackRange * 1.3f)
            {
                ChangeState(EnemyState.Chasing);
                return;
            }

            _agent.isStopped = true;
            LookAtPlayer();

            if (HasLineOfSight())
            {
                _lastSeenPlayerTime = Time.time;
                _lastKnownPlayerPosition = GetPlayerCenter();

                if (!_isAiming)
                {
                    _isAiming = true;
                    _aimTimer = 0f;
                }

                _aimTimer += Time.deltaTime;

                if (_aimTimer >= aimTime && weaponController && !weaponController.IsFiring)
                {
                    weaponController.StartFiring();
                }
            }
            else
            {
                StopAiming();
                if (Time.time - _lastSeenPlayerTime > 2f)
                    ChangeState(EnemyState.Searching);
            }
        }

        private void StopAiming()
        {
            _isAiming = false;
            _aimTimer = 0f;

            if (weaponController && weaponController.IsFiring)
                weaponController.StopFiring();
        }

        private void HandleSearching()
        {
            SetDestination(_lastKnownPlayerPosition);

            if (!_agent.pathPending && _agent.remainingDistance < 2f)
            {
                if (Time.time - _lastSeenPlayerTime > 5f)
                    ChangeState(EnemyState.Patrolling);
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            if (_currentHealth <= 0)
            {
                Die();
                return;
            }

            if (_animator)
                _animator.SetTrigger(TakeDamageHash);

            // Immediately engage if damaged
            if (_player)
            {
                _lastSeenPlayerTime = Time.time;
                _lastKnownPlayerPosition = GetPlayerCenter();

                float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
                ChangeState(distanceToPlayer <= attackRange ? EnemyState.Attacking : EnemyState.Chasing);
            }
        }

        private void Die()
        {
            ChangeState(EnemyState.Dead);

            if (_animator)
            {
                _animator.SetBool(IsCombat, false);
                _animator.SetBool(IsShooting, false);
                _animator.SetFloat(Speed, 0f);
                _animator.SetTrigger(DieHash);
            }

            if (weaponController)
                weaponController.StopFiring();

            DisableComponents();
            Destroy(gameObject, 3f);
        }

        private void DisableComponents()
        {
            if (weaponController)
                weaponController.enabled = false;

            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (!col.isTrigger)
                    col.enabled = false;
            }

            _agent.enabled = false;
        }

        private void ChangeState(EnemyState newState)
        {
            if (_currentState == newState) return;

            // Exit current state
            switch (_currentState)
            {
                case EnemyState.Attacking:
                    StopAiming();
                    break;
                case EnemyState.Patrolling:
                    _isWaiting = false;
                    break;
            }

            _currentState = newState;

            // Enter new state
            switch (newState)
            {
                case EnemyState.Patrolling:
                    _agent.speed = patrolSpeed;
                    _agent.isStopped = false;
                    _agent.updateRotation = true;
                    if (_patrolPoints != null && _patrolPoints.Length > 0)
                        MoveToNextPatrolPoint();
                    break;

                case EnemyState.Chasing:
                case EnemyState.Searching:
                    _agent.speed = combatSpeed;
                    _agent.isStopped = false;
                    _agent.updateRotation = false;
                    break;

                case EnemyState.Attacking:
                    _agent.speed = 0f;
                    _agent.updateRotation = false;
                    break;

                case EnemyState.Dead:
                    _agent.isStopped = true;
                    break;
            }
        }

        private Vector3 GetPlayerCenter()
        {
            return _player ? _player.position + Vector3.up * (playerHeight * 0.5f) : Vector3.zero;
        }

        private Vector3 GetPlayerHeadPosition()
        {
            if (_playerCamera)
                return _playerCamera.transform.position;

            return _player ? _player.position + Vector3.up * playerHeight : Vector3.zero;
        }

        private void LookAtPlayer()
        {
            if (!_player) return;

            Vector3 targetPosition = GetPlayerCenter();
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void SetDestination(Vector3 destination)
        {
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(destination);
            }
        }

        private void UpdateAnimations()
        {
            if (!_animator) return;

            bool isMoving = !_agent.isStopped && _agent.velocity.magnitude > 0.1f && _agent.hasPath;
            bool isCombat = _currentState != EnemyState.Patrolling;
            bool isShooting = _currentState == EnemyState.Attacking && _aimTimer >= aimTime;

            _animator.SetBool(IsCombat, isCombat);
            _animator.SetBool(IsShooting, isShooting);
            _animator.SetFloat(Speed, isMoving ? _agent.velocity.magnitude : 0f);
        }

        public void SetPatrolPoints(Transform[] points)
        {
            _patrolPoints = points;

            if (_patrolPoints?.Length > 0 && _agent.isActiveAndEnabled)
            {
                _currentPatrolIndex = Random.Range(0, _patrolPoints.Length);
                SetDestination(_patrolPoints[_currentPatrolIndex].position);
            }
        }

        private void SetupAgent()
        {
            _agent.speed = patrolSpeed;
            _agent.angularSpeed = angularSpeed;
            _agent.acceleration = acceleration;
            _agent.updateRotation = true;
            _agent.updatePosition = true;
            _agent.stoppingDistance = stopDistance;
        }

        private void RecoverAgent()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                return;
            }

            // Try to warp to nearest patrol point
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                Transform nearestPoint = _patrolPoints[_currentPatrolIndex];
                if (NavMesh.SamplePosition(nearestPoint.position, out NavMeshHit patrolHit, 2f, NavMesh.AllAreas))
                    _agent.Warp(patrolHit.position);
            }
        }

        // Public API for external control
        public void ForceEngagePlayer()
        {
            if (_player)
            {
                _lastSeenPlayerTime = Time.time;
                _lastKnownPlayerPosition = GetPlayerCenter();
                ChangeState(EnemyState.Chasing);
            }
        }

        public void SetPatrolType(PatrolType newType) => patrolType = newType;

        public float GetDistanceToPlayer() => _player ? Vector3.Distance(transform.position, _player.position) : float.MaxValue;
    }
}