using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// Simple enemy spawner with basic functionality
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
        
        private void Start()
        {
            _nextSpawnTime = Time.time + 2f; // Initial delay
        }
        
        private void Update()
        {
            if (ShouldSpawnEnemy())
            {
                SpawnRandomEnemy();
            }
        }
        
        private bool ShouldSpawnEnemy()
        {
            CleanupDestroyedEnemies();
            
            return _activeEnemies.Count < maxEnemies && 
                   Time.time >= _nextSpawnTime &&
                   enemyPrefab != null && 
                   spawnPoints != null && 
                   spawnPoints.Length > 0;
        }
        
        private void SpawnRandomEnemy()
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            
            var controller = enemy.GetComponent<EnemyController>();
            if (controller)
            {
                controller.SetPatrolPoints(GetRandomPatrolPoints());
            }
            
            _activeEnemies.Add(enemy);
            _nextSpawnTime = Time.time + spawnInterval;
        }
        
        private Transform[] GetRandomPatrolPoints()
        {
            int pointCount = Random.Range(minPatrolPoints, Mathf.Min(maxPatrolPoints + 1, spawnPoints.Length + 1));
            pointCount = Mathf.Clamp(pointCount, 1, spawnPoints.Length);
            
            var shuffledPoints = new List<Transform>(spawnPoints);
            
            // Simple shuffle
            for (int i = shuffledPoints.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                Transform temp = shuffledPoints[i];
                shuffledPoints[i] = shuffledPoints[randomIndex];
                shuffledPoints[randomIndex] = temp;
            }
            
            Transform[] result = new Transform[pointCount];
            for (int i = 0; i < pointCount; i++)
                result[i] = shuffledPoints[i];
            
            return result;
        }
        
        private void CleanupDestroyedEnemies()
        {
            _activeEnemies.RemoveAll(enemy => enemy == null);
        }
    }
}