using UnityEngine;

public class MagicCircle : MonoBehaviour
{
    public float duration = 5f;

    void Start()
    {
        Destroy(gameObject, duration);
    }

}
