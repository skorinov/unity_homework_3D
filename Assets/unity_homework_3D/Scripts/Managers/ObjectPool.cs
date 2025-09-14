using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // Find root object and make persistent
                Transform rootTransform = transform;
                while (rootTransform.parent != null)
                {
                    rootTransform = rootTransform.parent;
                }
                
                DontDestroyOnLoad(rootTransform.gameObject);
                InitializePools();
            }
            else
            {
                // Destroy duplicate
                Transform rootToDestroy = transform;
                while (rootToDestroy.parent != null)
                {
                    rootToDestroy = rootToDestroy.parent;
                }
                Destroy(rootToDestroy.gameObject);
            }
        }

        private void InitializePools()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                // Pre-instantiate objects
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab, transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                _poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist");
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

            // Pool is empty, create new object
            var pool = pools.Find(p => p.tag == tag);
            if (pool.prefab == null)
            {
                Debug.LogError($"No prefab found for pool tag {tag}");
                return null;
            }

            return Instantiate(pool.prefab, transform);
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
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist, destroying object");
                Destroy(objectToReturn);
                return;
            }

            objectToReturn.SetActive(false);
            objectToReturn.transform.SetParent(transform);
            _poolDictionary[tag].Enqueue(objectToReturn);
        }

        // Utility methods
        public bool HasPool(string tag) => _poolDictionary != null && _poolDictionary.ContainsKey(tag);

        public int GetPoolSize(string tag)
        {
            return _poolDictionary.ContainsKey(tag) ? _poolDictionary[tag].Count : 0;
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
    }

    // Interface for pooled objects to reset themselves
    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}