using Photon.Pun;
using UnityEngine;

public class Skill2Projectile : MonoBehaviour
{
    public float lifeTime = 8.0f;
    private void Start()
    {
        Destroy(gameObject,lifeTime);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Plane"))
        {
            Vector3 pos = transform.position;
            Debug.Log("plane");
            PhotonNetwork.Instantiate("Pen_Skill2_MagicCircle",pos,Quaternion.identity);
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
