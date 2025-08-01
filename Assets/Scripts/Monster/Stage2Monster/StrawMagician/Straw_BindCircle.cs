using Photon.Pun;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Straw_BindCircle : MonoBehaviour
{
    public int bindCircleCount = 6;
    public float spawnRadius = 15f;       // 보스 기준 공격 사거리
    public float bindCircleDamage = 5f;
    public float bindCircleDuration = 3f;
    public float bindDuration = 3.0f;
    public float minDistanceBetweenCircles = 5f;
    public float minDistanceFromPlayers = 2f;
    public Transform spawnCenter;       // 스폰 기준(지금은 보스 위치일 예정)
    public float reduceTime = 3.0f;
    Animator animator;
    public float cooldown = 8.0f;
    float lastAttackTime;
    PhotonView pv;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        pv=GetComponent<PhotonView>();
    }
    public bool IsReady()
    {
        return Time.time >= lastAttackTime + cooldown;
    }
    public bool IsInRange(float targetPos)
    {
        return targetPos <= spawnRadius;
    }
    [PunRPC]
    public void StrawMagician_BindCircleRPC()
    {



        /*if (Time.time < lastAttackTime + cooldown)
        {

            return;
        }*/
        lastAttackTime = Time.time;

        // 공격 애니메이션 트리거 
        if (animator != null)
        {
            animator.SetTrigger("Skill");
        }

        // 데미지를 주는건 마스터만

    }
    public void SpawnBindCircles()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        List<Vector3> validPositions = new List<Vector3>();         //알맞은 위치의 장판들
        List<Vector3> playerPositions = GetAllPlayerPositions();    //현 플레이어 위치
        int maxAttempts = 50;
        int attempts = 0;

        while (validPositions.Count < bindCircleCount && attempts < maxAttempts)
        {
            Vector3 candidate = GetRandomPositionAround(transform.position, spawnRadius);
            bool isValid = true;
            // 랜덤 위치와 기존 장판의 위치가 최소거리 이하면 break
            foreach (var pos in validPositions)
            {
                if (Vector3.Distance(candidate, pos) < minDistanceBetweenCircles)
                {
                    isValid = false;
                    break;
                }
            }
            // 랜덤 위치와 기존 플레이어의 위치가 최소 거리 이하면 break
            foreach (var p in playerPositions)
            {
                if (Vector3.Distance(candidate, p) < minDistanceFromPlayers)
                {
                    isValid = false;
                    break;
                }
            }
            // 조건 만족시 추가
            if (isValid)
                validPositions.Add(candidate);

            attempts++;
        }

        foreach (var pos in validPositions)
        {
            GameObject circle = PhotonNetwork.Instantiate("test/Straw_BindCircle", pos, Quaternion.identity);
            circle.GetComponent<PhotonView>().RPC("RPC_Initialize", RpcTarget.All, bindCircleDamage,bindCircleDuration,pv.ViewID,reduceTime);
        }
    }
    // 현재 보스 기준 공격 사거리 안의 랜덤 위치 리턴
    private Vector3 GetRandomPositionAround(Vector3 center, float radius)
    {
        Vector2 rand = Random.insideUnitCircle * radius;
        Vector3 pos = new Vector3(center.x + rand.x, center.y, center.z + rand.y);
        return pos;
    }
    // 현 플레이어 위치 검사
    List<Vector3> GetAllPlayerPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        int playerLayer = LayerMask.NameToLayer("Player");

        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.layer == playerLayer)
            {
                positions.Add(obj.transform.position);
            }
        }

        return positions;
    }
}
