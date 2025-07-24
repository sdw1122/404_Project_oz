using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AggroSystem : MonoBehaviour
{
    private Dictionary<GameObject, float> aggroDict = new Dictionary<GameObject, float>();

    // 플레이어에게 받은 데미지 추가
    public void AddAggro(GameObject player,float value)
    {
        if (player == null) return;
        if (!aggroDict.ContainsKey(player))
        {
            aggroDict[player] = 0f;
        }
        aggroDict[player] += value;
    }
    // 가장 큰 데미지를 준 플레이어 반환
    public GameObject GetTopAggroTarget()
    {
        if (aggroDict.Count == 0) return null;
        Debug.Log($"가장 데미지를 많이 준 플레이어 : {aggroDict.OrderByDescending(pair => pair.Value).First().Key}" +
            $"누적 데미지 : {aggroDict.OrderByDescending(pair => pair.Value).First().Value}");

        return aggroDict.OrderByDescending(pair => pair.Value).First().Key;
    }
    // 사망한 플레이어의 누적 피해량은 0으로 초기화되고, 생존한 플레이어의 누적 피해량은 절반으로 감소
    [PunRPC]
    public void ResetAndHalfAggro(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GameObject Deadplayer = PhotonView.Find(viewID)?.gameObject;
        foreach (var key in aggroDict.Keys.ToList())
        {
            if (key == Deadplayer)
            {
                aggroDict[key] = 0.0f;
                Debug.Log($"{Deadplayer}의 누적 데미지 : {aggroDict[key]}");
            }
                
            else if (key != Deadplayer) 
            {
                aggroDict[key] *= 0.5f;
                Debug.Log($"상대방 플레이어의 누적 데미지 : {aggroDict[key]}");
            }
                
        }
    }
    // 리셋
    public void Reset()
    {
        aggroDict.Clear();
    }
}
