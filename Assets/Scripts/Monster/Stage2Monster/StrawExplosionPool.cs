using System.Collections.Generic;
using UnityEngine;

public class StrawExplosionPool : MonoBehaviour
{
    public static StrawExplosionPool instance {  get; private set; }
    public GameObject explosionPrefab;
    public int count = 20;
    public float delay = 10f;

    private Queue<GameObject> explosionPool = new Queue<GameObject>();
    private void Awake()
    {
        instance = this;

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(explosionPrefab, transform);
            go.SetActive(false);
            explosionPool.Enqueue(go);
        }
    }

    public GameObject GetExp(Vector3 pos)
    {
        GameObject go = explosionPool.Count > 0 ? explosionPool.Dequeue() : Instantiate(explosionPrefab, transform);
        go.transform.SetPositionAndRotation(pos, explosionPrefab.transform.rotation);
        go.SetActive(true);
        StartCoroutine(ReturnPool(go, explosionPool, delay));
        return go;
    }

    private System.Collections.IEnumerator ReturnPool(GameObject go, Queue<GameObject> pool, float t)
    {
        yield return new WaitForSeconds(t);
        if (go != null)
        {
            go.SetActive(false);
            explosionPool.Enqueue(go);
        }
    }
}
