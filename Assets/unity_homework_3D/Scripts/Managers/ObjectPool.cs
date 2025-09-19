using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Persistent object pool that properly handles game restarts
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }

        [Header("Pool Configuration")]
        public List<Pool> pools;

        private Dictionary<string, Queue<GameObject>> _poolDictionary;
        private Dictionary<string, GameObject> _poolPrefabs;
        private bool _isInitialized = false;

        private void Awake()
        {
            // Persistent singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
                SubscribeToGameEvents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SubscribeToGameEvents()
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.OnGameRestarted += OnGameRestarted;
            }
        }

        private void InitializePools()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
            _poolPrefabs = new Dictionary<string, GameObject>();

            foreach (Pool pool in pools)
            {
                if (pool.prefab == null)
                {
                    Debug.LogWarning($"[ObjectPool] Prefab is null for pool: {pool.tag}");
                    continue;
                }

                Queue<GameObject> objectPool = new Queue<GameObject>();
                _poolPrefabs[pool.tag] = pool.prefab;

                // Pre-instantiate objects
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab, transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                _poolDictionary.Add(pool.tag, objectPool);
            }

            _isInitialized = true;
        }

        private void OnGameRestarted()
        {
            CleanupActiveObjects();
        }

        private void CleanupActiveObjects()
        {
            if (!_isInitialized) return;

            // Return all active pooled objects back to their pools
            foreach (var kvp in _poolDictionary)
            {
                string poolTag = kvp.Key;
                Queue<GameObject> pool = kvp.Value;

                // Find all active objects of this pool type in the scene
                var activeObjects = FindActivePooledObjects(poolTag);
                
                foreach (var obj in activeObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        obj.transform.SetParent(transform);
                        
                        // Reset pooled object
                        var pooledComponent = obj.GetComponent<IPooledObject>();
                        pooledComponent?.OnObjectSpawn();
                        
                        pool.Enqueue(obj);
                    }
                }
            }
        }

        private List<GameObject> FindActivePooledObjects(string poolTag)
        {
            List<GameObject> activeObjects = new List<GameObject>();
            
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var obj in allObjects)
            {
                // Check if object belongs to this pool (by name or component)
                if (obj.name.Contains(_poolPrefabs[poolTag].name) && obj.activeInHierarchy)
                {
                    var pooledComponent = obj.GetComponent<IPooledObject>();
                    if (pooledComponent != null)
                    {
                        activeObjects.Add(obj);
                    }
                }
            }
            
            return activeObjects;
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[ObjectPool] Pool not initialized yet!");
                return null;
            }

            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool with tag {tag} doesn't exist");
                return null;
            }

            GameObject objectToSpawn = GetPooledObject(tag);
            if (!objectToSpawn) return null;

            SetupSpawnedObject(objectToSpawn, position, rotation);
            return objectToSpawn;
        }

        // Generic typed version - more efficient
        public T SpawnFromPool<T>(string tag, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject obj = SpawnFromPool(tag, position, rotation);
            return obj?.GetComponent<T>();
        }

        // Overload for direct component initialization
        public T SpawnFromPool<T>(string tag, Vector3 position, Quaternion rotation, System.Action<T> initializeAction) where T : Component
        {
            T component = SpawnFromPool<T>(tag, position, rotation);
            initializeAction?.Invoke(component);
            return component;
        }

        private GameObject GetPooledObject(string tag)
        {
            if (_poolDictionary[tag].Count > 0)
            {
                return _poolDictionary[tag].Dequeue();
            }

            // Pool is empty, create new object if we have the prefab
            if (_poolPrefabs.ContainsKey(tag) && _poolPrefabs[tag])
            {
                return Instantiate(_poolPrefabs[tag], transform);
            }

            Debug.LogError($"[ObjectPool] No prefab found for pool tag {tag}");
            return null;
        }

        private void SetupSpawnedObject(GameObject obj, Vector3 position, Quaternion rotation)
        {
            obj.SetActive(true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.SetParent(null); // Remove from pool parent when active

            // Reset pooled object
            obj.GetComponent<IPooledObject>()?.OnObjectSpawn();
        }

        public void ReturnToPool(string tag, GameObject objectToReturn)
        {
            if (!_isInitialized) return;

            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool with tag {tag} doesn't exist, destroying object");
                Destroy(objectToReturn);
                return;
            }

            if (!objectToReturn) return;

            objectToReturn.SetActive(false);
            objectToReturn.transform.SetParent(transform);
            _poolDictionary[tag].Enqueue(objectToReturn);
        }

        // Utility methods
        public bool HasPool(string tag) => _poolDictionary != null && _poolDictionary.ContainsKey(tag);

        public int GetPoolSize(string tag)
        {
            return _poolDictionary != null && _poolDictionary.ContainsKey(tag) ? _poolDictionary[tag].Count : 0;
        }

        public int GetActiveObjectCount(string tag)
        {
            if (!_poolPrefabs.ContainsKey(tag)) return 0;
            return FindActivePooledObjects(tag).Count;
        }

        // Spawn object that auto-returns to pool after specified time
        public GameObject SpawnFromPoolTimed(string tag, Vector3 position, Quaternion rotation, float lifetime)
        {
            GameObject obj = SpawnFromPool(tag, position, rotation);
            if (obj)
            {
                StartCoroutine(ReturnAfterDelay(tag, obj, lifetime));
            }
            return obj;
        }
        
        // Generic typed version with auto-return
        public T SpawnFromPoolTimed<T>(string tag, Vector3 position, Quaternion rotation, float lifetime) where T : Component
        {
            GameObject obj = SpawnFromPoolTimed(tag, position, rotation, lifetime);
            return obj?.GetComponent<T>();
        }
        
        // Typed version with initialization and auto-return
        public T SpawnFromPoolTimed<T>(string tag, Vector3 position, Quaternion rotation, float lifetime, System.Action<T> initializeAction) where T : Component
        {
            T component = SpawnFromPoolTimed<T>(tag, position, rotation, lifetime);
            initializeAction?.Invoke(component);
            return component;
        }
        
        private System.Collections.IEnumerator ReturnAfterDelay(string tag, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (obj) // Check if object still exists
            {
                ReturnToPool(tag, obj);
            }
        }

        // Clear all pools
        public void ClearAllPools()
        {
            if (!_isInitialized) return;

            foreach (var kvp in _poolDictionary)
            {
                var pool = kvp.Value;
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj) Destroy(obj);
                }
            }

            // Also destroy any active pooled objects
            foreach (var poolTag in _poolPrefabs.Keys)
            {
                var activeObjects = FindActivePooledObjects(poolTag);
                foreach (var obj in activeObjects)
                {
                    if (obj) Destroy(obj);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (GameManager.Instance)
            {
                GameManager.Instance.OnGameRestarted -= OnGameRestarted;
            }
        }
    }

    // Interface for pooled objects to reset themselves
    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}