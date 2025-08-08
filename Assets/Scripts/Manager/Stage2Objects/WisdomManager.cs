using Photon.Pun;
using TMPro;
using UnityEngine;

public class WisdomManager : MonoBehaviour
{
    public static WisdomManager Instance;

    
    public int requiredWisdom = 100; 

    private PhotonView pv;
    public int currentWisdom = 0;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        pv=GetComponent<PhotonView>();
    }
    public int GetCurrentWisdom()
    {
        return currentWisdom;
    }
    // 몬스터에게서 받아오기
    public void AddWisdom(int amount)
    {
        if(!PhotonNetwork.IsMasterClient) { return; }
        int newWisdom = currentWisdom + amount;
        if (newWisdom >= 100) newWisdom = 100;
        pv.RPC(nameof(RPC_UpdateWisdom), RpcTarget.All, newWisdom);
    }
    // 마스터만 호출해야
    public void UseWisdom(int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int newWisdom = currentWisdom - amount;
        if (newWisdom < 0) newWisdom = 0;
        pv.RPC(nameof(RPC_UpdateWisdom), RpcTarget.All, newWisdom);
    }
    // 지혜 수치 동기화
    [PunRPC]
    private void RPC_UpdateWisdom(int newWisdom)
    {
        currentWisdom = newWisdom;
    }
    
}
