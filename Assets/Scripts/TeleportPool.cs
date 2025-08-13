using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPool : MonoBehaviour
{
    public static TeleportPool Instance { get; private set; }
    public GameObject teleportBeforePrefab;
    public GameObject teleportAfterPrefab;
    public int beforeInitialCount = 20;
    public int afterInitialCount = 20;
    public float delay = 10.0f;

    private Queue<GameObject> beforePool = new Queue<GameObject>();
    private Queue<GameObject> afterPool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
        
        for (int i = 0; i < beforeInitialCount; i++)
        {
            var go = Instantiate(teleportBeforePrefab, transform);
            go.SetActive(false);
            beforePool.Enqueue(go);
        }
        for (int i = 0; i < afterInitialCount; i++)
        {
            var go = Instantiate(teleportAfterPrefab, transform);
            go.SetActive(false);
            afterPool.Enqueue(go);
        }
    }
    public GameObject GetBefore(Vector3 pos)
    {
        GameObject go = beforePool.Count > 0 ? beforePool.Dequeue() : Instantiate(teleportBeforePrefab, transform);
        go.transform.SetPositionAndRotation(pos, teleportBeforePrefab.transform.rotation);
        go.SetActive(true);
        StartCoroutine(ReturnPool(go, beforePool, delay));
        return go;
    }
    public GameObject GetAfter(Vector3 pos)
    {
        GameObject go = afterPool.Count > 0 ? afterPool.Dequeue() : Instantiate(teleportAfterPrefab, transform);
        go.transform.SetPositionAndRotation(pos, teleportAfterPrefab.transform.rotation);
        go.SetActive(true);
        StartCoroutine(ReturnPool(go, afterPool, delay));
        return go;
    }
    private System.Collections.IEnumerator ReturnPool(GameObject go, Queue<GameObject> pool, float t)
    {
        yield return new WaitForSeconds(t);
        if (go != null)
        {
            go.SetActive(false);
            beforePool.Enqueue(go);
        }
    }
}
