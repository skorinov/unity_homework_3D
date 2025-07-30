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
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                
                // For DontDestroyOnLoad we need to work with the root object
                // Find the root object (Managers) and make it persistent
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
                // Destroy the entire root object if duplicate
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

            GameObject objectToSpawn;
            
            if (_poolDictionary[tag].Count > 0)
            {
                objectToSpawn = _poolDictionary[tag].Dequeue();
            }
            else
            {
                // Pool is empty, create new object
                var pool = pools.Find(p => p.tag == tag);
                if (pool.prefab == null)
                {
                    Debug.LogError($"No prefab found for pool tag {tag}");
                    return null;
                }
                objectToSpawn = Instantiate(pool.prefab, transform);
            }

            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.transform.SetParent(null); // Remove from pool parent when active

            // Reset any pooled object components
            var pooledObject = objectToSpawn.GetComponent<IPooledObject>();
            pooledObject?.OnObjectSpawn();

            return objectToSpawn;
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

        // Utility method to check if pool exists
        public bool HasPool(string tag)
        {
            return _poolDictionary != null && _poolDictionary.ContainsKey(tag);
        }

        // Utility method to get pool size
        public int GetPoolSize(string tag)
        {
            if (!_poolDictionary.ContainsKey(tag)) return 0;
            return _poolDictionary[tag].Count;
        }
    }

    // Interface for pooled objects to reset themselves
    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}