using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
    }
    
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    
    private static ObjectPool instance;
    public static ObjectPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ObjectPool>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ObjectPool");
                    instance = obj.AddComponent<ObjectPool>();
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializePools();
    }
    
    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        
        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogWarning($"Pool '{pool.tag}' has no prefab assigned!");
                continue;
            }
            
            Queue<GameObject> objectPool = new Queue<GameObject>();
            
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            
            poolDictionary.Add(pool.tag, objectPool);
        }
    }
    
    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist!");
            return null;
        }
        
        GameObject objectToSpawn;
        
        if (poolDictionary[tag].Count > 0)
        {
            objectToSpawn = poolDictionary[tag].Dequeue();
        }
        else
        {
            Pool pool = pools.Find(p => p.tag == tag);
            objectToSpawn = Instantiate(pool.prefab, transform);
        }
        
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);
        
        return objectToSpawn;
    }
    
    public void Despawn(string tag, GameObject obj, float delay = 0f)
    {
        if (delay > 0f)
        {
            StartCoroutine(DespawnAfterDelay(tag, obj, delay));
        }
        else
        {
            DespawnImmediate(tag, obj);
        }
    }
    
    private System.Collections.IEnumerator DespawnAfterDelay(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        DespawnImmediate(tag, obj);
    }
    
    private void DespawnImmediate(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Destroy(obj);
            return;
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        poolDictionary[tag].Enqueue(obj);
    }
}
