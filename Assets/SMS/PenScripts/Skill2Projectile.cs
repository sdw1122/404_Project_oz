using Photon.Pun;
using System.Reflection;
using UnityEngine;

public class Skill2Projectile : MonoBehaviour
{
    public float lifeTime = 8.0f;
    public float damage;
    public float tik;
    private void Start()
    {
        Destroy(gameObject,lifeTime);
    }
    public void Initialize(float p_damage, float p_tik)
    {
        damage = p_damage;
        tik = p_tik;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Vector3 pos = transform.position;
            Debug.Log("plane");
            GameObject magicCircle =  PhotonNetwork.Instantiate("Pen_Skill2_MagicCircle",pos,Quaternion.identity);
            magicCircle.GetComponent<MagicCircle>().Initialize(damage,tik);
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
