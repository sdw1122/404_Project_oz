using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifetime = 1.0f;
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}