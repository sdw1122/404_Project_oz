using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// [1인용 테스트 버전] 한 명의 플레이어가 상호작용하여 활성화하는 장치입니다.
/// 활성화된 동안 몬스터를 소환하며, 플레이어가 가까이 있어야 비활성화됩니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class CooperativePurifier : InteractableBase
{
    [Header("활성화/비활성화 대상")]
    [Tooltip("활성화 시 켤 오브젝트나 파티클 효과 등을 연결합니다.")]
    [SerializeField] private GameObject activationVisual;

    // ▼▼▼ [수정] 1인용 테스트에서는 이 값이 사용되지 않습니다. ▼▼▼
    [Header("정화(비활성화) 설정")]
    [Tooltip("[2인용 설정] 두 플레이어가 이 거리 안에 있어야 정화가 진행됩니다.")]
    [SerializeField] private float deactivationRange = 5f;
    [Tooltip("정화를 완료하는 데 필요한 시간(초)입니다.")]
    [SerializeField] private float timeToDeactivate = 10f;

    [Header("초기화 설정")]
    [Tooltip("활성화 후, 플레이어가 이 오브젝트로부터 이 거리 이상 벗어나면 초기화됩니다.")]
    [SerializeField] private float maxPlayerDistance = 30f;

    private string resourcePath = "Model/Prefab/Stage2/";
    [Header("몬스터 소환 설정")]
    [Tooltip("소환할 몬스터 프리팹 ('Resources' 폴더 안에 있어야 함)")]
    [SerializeField] private GameObject monsterPrefab;
    [Tooltip("몬스터가 소환될 위치들")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("몬스터 소환 간격(초)")]
    [SerializeField] private float spawnInterval = 5f;

    private bool isActivated = false;
    private float deactivationTimer = 0f;
    private List<int> interactingPlayerIDs = new List<int>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    private void Start()
    {
        if (activationVisual != null)
        {
            activationVisual.SetActive(false);
        }
    }

    public override void Interact(PlayerController player)
    {
        if (isActivated) return;
        pv.RPC("RPC_RegisterInteraction", RpcTarget.MasterClient, player.GetComponent<PhotonView>().Owner.ActorNumber);
    }

    [PunRPC]
    private void RPC_RegisterInteraction(int playerActorNumber)
    {
        if (isActivated) return;

        if (!interactingPlayerIDs.Contains(playerActorNumber))
        {
            interactingPlayerIDs.Add(playerActorNumber);
        }

        // ▼▼▼ [수정] 1인용으로 활성화 조건을 변경합니다. ▼▼▼
        // if (interactingPlayerIDs.Count >= 2) // 기존 2인용 코드
        if (interactingPlayerIDs.Count >= 1) // 1인용 테스트 코드
        {
            pv.RPC("RPC_ActivateDevice", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void RPC_ActivateDevice()
    {
        if (isActivated) return;
        isActivated = true;
        deactivationTimer = timeToDeactivate;

        if (activationVisual != null)
        {
            activationVisual.SetActive(true);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ActiveStateRoutine());
        }
    }

    private IEnumerator ActiveStateRoutine()
    {
        float spawnTimer = spawnInterval;

        while (isActivated)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnMonster();
                spawnTimer = spawnInterval;
            }

            // ▼▼▼ [수정] 플레이어 유효성 및 거리 체크 로직을 1인용으로 변경합니다. ▼▼▼
            Player player1 = GetPlayerByActorNumber(interactingPlayerIDs[0]);
            GameObject player1Obj = FindPlayerObject(player1);

            // 플레이어가 나가거나, 너무 멀리 떨어지면 초기화
            if (player1Obj == null || Vector3.Distance(transform.position, player1Obj.transform.position) > maxPlayerDistance)
            {
                Debug.Log("플레이어가 범위를 벗어나 장치를 초기화합니다.");
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            // ▼▼▼ [수정] 정화(비활성화) 조건 체크 로직을 1인용으로 변경합니다. ▼▼▼
            // 플레이어 간 거리 체크 로직을 제거하고, 플레이어가 존재하기만 하면 타이머가 흐르도록 합니다.
            deactivationTimer -= Time.deltaTime;
            if (deactivationTimer <= 0)
            {
                Debug.Log("정화 완료! 장치를 비활성화합니다.");
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            /* // 기존 2인용 코드
            if (Vector3.Distance(player1Obj.transform.position, player2Obj.transform.position) <= deactivationRange)
            {
                deactivationTimer -= Time.deltaTime;
                if (deactivationTimer <= 0)
                {
                    Debug.Log("정화 완료! 장치를 비활성화합니다.");
                    pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                    yield break; 
                }
            }
            else
            {
                deactivationTimer = timeToDeactivate;
            }
            */

            yield return null;
        }
    }

    [PunRPC]
    private void RPC_ResetDevice()
    {
        isActivated = false;
        if (activationVisual != null)
        {
            activationVisual.SetActive(false);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            interactingPlayerIDs.Clear();
            foreach (var monster in spawnedMonsters)
            {
                if (monster != null)
                {
                    PhotonNetwork.Destroy(monster);
                }
            }
            spawnedMonsters.Clear();
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null || spawnPoints.Length == 0) return;
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject monster = PhotonNetwork.Instantiate(resourcePath + monsterPrefab.name, spawnPoint.position, spawnPoint.rotation);
        spawnedMonsters.Add(monster);
    }

    private Player GetPlayerByActorNumber(int actorNumber)
    {
        return PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
    }

    private GameObject FindPlayerObject(Player player)
    {
        if (player == null) return null;
        foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (p.GetComponent<PhotonView>().Owner.ActorNumber == player.ActorNumber)
            {
                return p.gameObject;
            }
        }
        return null;
    }
}