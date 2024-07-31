using System.Collections.Generic;
using UnityEngine;

public class BrickPool : MonoBehaviour
{
    public static BrickPool Instance;
    public GameObject objectToPool;
    public int poolSize;
    private List<GameObject> pooledObjects;
    private HashSet<GameObject> activeObjects;

    private void Awake()
    {
        Instance = this;
        pooledObjects = new List<GameObject>();
        activeObjects = new HashSet<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(objectToPool, this.transform);
            obj.SetActive(false);
            pooledObjects.Add(obj);
        }
    }

    public GameObject GetPooledObject()
    {
        foreach (GameObject obj in pooledObjects)
        {
            if (!obj.activeInHierarchy)
            {
                activeObjects.Add(obj);
                return obj;
            }
        }

        // Optionally, expand the pool if needed
        GameObject newObj = Instantiate(objectToPool);
        newObj.SetActive(false);
        pooledObjects.Add(newObj);
        activeObjects.Add(newObj);
        return newObj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        activeObjects.Remove(obj);
    }

    public int ActiveCount()
    {
        return activeObjects.Count;
    }
}
