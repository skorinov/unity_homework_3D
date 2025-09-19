using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// Enemy spawner that properly reinitializes on game restart
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int maxEnemies = 10;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int minPatrolPoints = 3;
        [SerializeField] private int maxPatrolPoints = 6;
        
        private List<GameObject> _activeEnemies = new List<GameObject>();
        private float _nextSpawnTime;
        private bool _isInitialized = false;
        
        private void Start()
        {
            Initialize();
            SubscribeToGameEvents();
        }
        
        private void Initialize()
        {
            _activeEnemies.Clear();
            _nextSpawnTime = Time.time + 2f; // Initial delay
            _isInitialized = true;
        }
        
        private void SubscribeToGameEvents()
        {
            if (Managers.GameManager.Instance)
            {
                Managers.GameManager.Instance.OnGameRestarted += OnGameRestarted;
                Managers.GameManager.Instance.OnGamePaused += OnGamePaused;
                Managers.GameManager.Instance.OnGameResumed += OnGameResumed;
            }
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            if (Managers.GameManager.Instance && Managers.GameManager.Instance.IsPaused) return;
            
            if (ShouldSpawnEnemy())
            {
                SpawnRandomEnemy();
            }
        }
        
        private void OnGameRestarted()
        {
            Initialize();
        }
        
        private void OnGamePaused()
        {
            // Pause all enemy activities if needed
        }
        
        private void OnGameResumed()
        {
            // Resume enemy activities if needed
        }
        
        private bool ShouldSpawnEnemy()
        {
            CleanupDestroyedEnemies();
            
            return _activeEnemies.Count < maxEnemies && 
                   Time.time >= _nextSpawnTime &&
                   enemyPrefab && 
                   HasValidSpawnPoints();
        }
        
        private bool HasValidSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return false;
                
            foreach (var point in spawnPoints)
            {
                if (point)
                    return true;
            }
            return false;
        }
        
        private void SpawnRandomEnemy()
        {
            Transform spawnPoint = GetRandomValidSpawnPoint();
            
            if (!spawnPoint)
            {
                return;
            }
            
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            
            var controller = enemy.GetComponent<EnemyController>();
            if (controller)
            {
                controller.SetPatrolPoints(GetRandomPatrolPoints());
            }
            
            _activeEnemies.Add(enemy);
            _nextSpawnTime = Time.time + spawnInterval;
        }
        
        private Transform GetRandomValidSpawnPoint()
        {
            List<Transform> validPoints = new List<Transform>();
            
            if (spawnPoints == null) return null;
            
            foreach (var point in spawnPoints)
            {
                if (point)
                    validPoints.Add(point);
            }
            
            if (validPoints.Count == 0)
                return null;
                
            return validPoints[Random.Range(0, validPoints.Count)];
        }
        
        private Transform[] GetRandomPatrolPoints()
        {
            var validPoints = new List<Transform>();
            
            if (spawnPoints == null) return new Transform[0];
            
            foreach (var point in spawnPoints)
            {
                if (point != null)
                    validPoints.Add(point);
            }
            
            if (validPoints.Count == 0)
                return new Transform[0];
            
            int pointCount = Random.Range(minPatrolPoints, Mathf.Min(maxPatrolPoints + 1, validPoints.Count + 1));
            pointCount = Mathf.Clamp(pointCount, 1, validPoints.Count);
            
            // Shuffle
            for (int i = validPoints.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                Transform temp = validPoints[i];
                validPoints[i] = validPoints[randomIndex];
                validPoints[randomIndex] = temp;
            }
            
            Transform[] result = new Transform[pointCount];
            for (int i = 0; i < pointCount; i++)
                result[i] = validPoints[i];
            
            return result;
        }
        
        private void CleanupDestroyedEnemies()
        {
            _activeEnemies.RemoveAll(enemy => enemy == null);
        }
        
        public void ClearAllEnemies()
        {
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null)
                    Destroy(enemy);
            }
            _activeEnemies.Clear();
        }
        
        private void OnDestroy()
        {
            if (Managers.GameManager.Instance)
            {
                Managers.GameManager.Instance.OnGameRestarted -= OnGameRestarted;
                Managers.GameManager.Instance.OnGamePaused -= OnGamePaused;
                Managers.GameManager.Instance.OnGameResumed -= OnGameResumed;
            }
        }
    }
}