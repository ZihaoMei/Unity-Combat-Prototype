using UnityEngine;
using System.Collections.Generic;

public class SimpleObjectPool : MonoBehaviour
{
    public static SimpleObjectPool Instance;
    public GameObject bloodPrefab; // 拖入你的黑血粒子预制体
    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake() { Instance = this; }

    public GameObject GetBlood(Vector3 position, Quaternion rotation)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else
        {
            return Instantiate(bloodPrefab, position, rotation);
        }
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}