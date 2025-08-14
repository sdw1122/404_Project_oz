using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Straw_Teleport : MonoBehaviour
{
    public Transform[] teleportPoints;
    public float teleportCooldown = 10.0f;
    float lastTeleportTime;
    // 데미지 받을시 쿨타임
    public float damagedTeleportCooldown = 5.0f;
    int lastTeleportIndex = -1;
    bool isDamaged = false;
    NavMeshAgent navMeshAgent;
    StrawMagician strawMagician;
    
    private void Awake()
    {
        lastTeleportTime = teleportCooldown;
        navMeshAgent = GetComponent<NavMeshAgent>();
        strawMagician = GetComponent<StrawMagician>();
    }
    public bool IsReady()
    {

        return Time.time > lastTeleportTime + teleportCooldown;
    }
    [PunRPC]
    public void PerformTeleportRPC()
    {
        SpawnBeTel();
        StartCoroutine(DelayTelSpawn());
        if (PhotonNetwork.IsMasterClient)
        {
            lastTeleportTime = Time.time; 
            isDamaged = false; // 새 텔레포트 주기 시작
            Debug.Log($"[Straw_Teleport] 마스터: 텔레포트 쿨타임 초기화");
            TeleportToRandomPoint();
            strawMagician.targetEntity = null;
        }
    }
    public void TeleportToRandomPoint()
    {
        if (teleportPoints.Length == 0) return;

        int newIndex = lastTeleportIndex;

        // teleportPoints가 1개면 무한 루프 방지
        if (teleportPoints.Length == 1)
        {
            newIndex = 0;
        }
        else
        {
            int maxAttempts = 10;
            int attempts = 0;

            // 이전 인덱스와 다른 위치가 선택될 때까지 반복
            while (newIndex == lastTeleportIndex && attempts < maxAttempts)
            {
                newIndex = Random.Range(0, teleportPoints.Length);
                attempts++;
            }
        }

        Vector3 newPosition = teleportPoints[newIndex].position;

        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Warp(newPosition);
            strawMagician.pv.RPC("RPC_ForceStrawSyncPosition", RpcTarget.Others,newPosition);
            lastTeleportIndex = newIndex;
            isDamaged = false;
        }
    }
    [PunRPC]
    public void RPC_ForceStrawSyncPosition(Vector3 pos)
    {
        navMeshAgent.Warp(pos); // 즉시 위치 일치
        transform.position = pos;
    }
    public void ReduceTeleportCooldown()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log("쿨타임 감소 요청됨"+isDamaged);
        // 이번 텔레포트 주기에서 아직 대미지 리셋을 사용하지 않았다면
        if (!isDamaged)
        {
            lastTeleportTime = Time.time - (teleportCooldown - damagedTeleportCooldown);
            isDamaged= true;
        }
    }
    public void SpawnBeTel()
    {
        Vector3 pos = transform.position;
        var go = TeleportPool.Instance.GetBefore(pos);
    }
    public void SpawnAfTel()
    {
        Vector3 pos = transform.position;
        var go = TeleportPool.Instance.GetAfter(pos);
    }
    private IEnumerator DelayTelSpawn()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnAfTel();
    } 
}
