using Photon.Pun;
using System.Reflection;
using UnityEngine;

public class Skill2Projectile : MonoBehaviour
{
    PhotonView pv;
    public float lifeTime = 8.0f;
    public float damage;
    public float tik;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
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
    {   if (!pv.IsMine) return;
        if (collision.gameObject.CompareTag("Ground"))
        {
            Vector3 pos = transform.position;
            Debug.Log("plane");
            GameObject magicCircle =  PhotonNetwork.Instantiate("Pen_Skill2_MagicCircle",pos,Quaternion.identity);
            magicCircle.GetComponent<PhotonView>().RPC("RPC_Initialize", RpcTarget.AllBuffered, damage, tik);
            if (pv != null && pv.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
