using Photon.Pun;
using UnityEngine;

public class WisdomCannon : InteractableBase
{
    public Transform firePoint;
    public GameObject cannonballPrefab;
    public float cannonDamage=400f;
    public float cannonballSpeed = 20f;
    protected override void Awake()
    {
        base.Awake();
        
    }
    public override void Interact(PlayerController player)
    {
        pv.RPC("TryFireWisdom", RpcTarget.MasterClient);
    }
    [PunRPC]
    public void TryFireWisdom()
    {
        if(WisdomManager.Instance.GetCurrentWisdom()>=WisdomManager.Instance.requiredWisdom)
        {
            WisdomManager.Instance.UseWisdom(WisdomManager.Instance.requiredWisdom);
            pv.RPC(nameof(FireWisdom), RpcTarget.MasterClient);
        }
        else
        {
            Debug.Log("지혜가 부족합니다");
        }
    }
    [PunRPC]
    public void FireWisdom()
    {
        GameObject cannonball = PhotonNetwork.Instantiate("test/" + cannonballPrefab.name, firePoint.position, Quaternion.identity);
        WisdomCannonBall ecb = cannonball.GetComponent<WisdomCannonBall>();
        Rigidbody rb = cannonball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            ecb.Initialize(cannonDamage);
            rb.AddForce(firePoint.forward * cannonballSpeed, ForceMode.VelocityChange);
        }
    }
}
