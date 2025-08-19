using Photon.Pun;
using UnityEngine;

public class ChargedPenMissileEffect : MonoBehaviour
{
    public float lifeTime = 7.0f;
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
