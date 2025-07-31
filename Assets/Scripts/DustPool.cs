using System.Collections.Generic;
using UnityEngine;

public class DustPool : MonoBehaviour
{
    public static DustPool Instance { get; private set; }
    public GameObject dustPrefab;
    public int initialCount = 10;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < initialCount; i++)
        {
            var go = Instantiate(dustPrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public GameObject GetDust(Vector3 pos, Quaternion rot)
    {
        GameObject go = pool.Count > 0 ? pool.Dequeue() : Instantiate(dustPrefab, transform);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void ReturnDust(GameObject go, float delay)
    {
        StartCoroutine(ReturnAfter(go, delay));
    }

    private System.Collections.IEnumerator ReturnAfter(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        go.SetActive(false);
        pool.Enqueue(go);
    }
}
