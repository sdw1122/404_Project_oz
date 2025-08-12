using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.AI;

/// <summary>
/// [몬스터 스폰 반경 통합 버전] 한 명 이상의 플레이어가 협력하여 활성화하는 장치입니다. (수정됨)
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class CooperativePurifier : InteractableBase, IPunObservable
{
    [Header("UI 설정")]
    [Tooltip("정화 장치의 자식으로 있는 UI Canvas 오브젝트를 연결하세요.")]
    [SerializeField] private GameObject purifierUICanvas;
    [Tooltip("Canvas 안에 있는 Slider를 연결하세요.")]
    [SerializeField] private Slider progressSlider;
    private Camera localPlayerCamera;

    [Header("활성화/비활성화 대상")]
    [SerializeField] private GameObject activationVisual;

    [Header("정화(비활성화) 설정")]
    [SerializeField] private float deactivationRange = 5f;
    [SerializeField] private float timeToDeactivate = 10f;

    [Header("초기화 및 스폰 반경 설정")]
    [Tooltip("플레이어가 벗어날 수 있는 최대 거리이자, 몬스터가 소환되는 반경입니다.")]
    [SerializeField] private float maxPlayerDistance = 30f;

    [Header("몬스터 소환 설정")]
    [Tooltip("장치 활성화 시 처음에 소환될 몬스터 리스트입니다.")]
    [SerializeField] private List<GameObject> initialSpawnMonsters;
    [SerializeField] private int initialSpawnCount;

    [Tooltip("초기 소환 이후, 지속적으로 소환될 몬스터 리스트입니다.")]
    [SerializeField] private List<GameObject> continuousMonsterPrefabs;

    [Tooltip("지속 소환 간격(초)입니다.")]
    [SerializeField] private float spawnInterval = 5f;

    [SerializeField] private GameObject objectToDestroy;

        

    private string resourcePath = "Model/Prefab/Stage2/";
    private bool isActivated = false;
    private float deactivationTimer = 0f;
    private List<int> interactingPlayerIDs = new List<int>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    private float networkDeactivationTimer = 0f;

    private bool isCompleted = false; // 정화 완료 여부

    void Start()
    {
        if (activationVisual != null) activationVisual.SetActive(false);
        if (purifierUICanvas != null) purifierUICanvas.SetActive(false);
    }

    private void Update()
    {
        if (isActivated && purifierUICanvas != null)
        {
            if (localPlayerCamera == null)
            {
                PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (p.GetComponent<PhotonView>().IsMine)
                    {
                        localPlayerCamera = p.GetComponentInChildren<Camera>();
                        break;
                    }
                }
            }

            if (localPlayerCamera != null)
            {
                purifierUICanvas.transform.LookAt(purifierUICanvas.transform.position + localPlayerCamera.transform.rotation * Vector3.forward, localPlayerCamera.transform.rotation * Vector3.up);
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                UpdateSlider(networkDeactivationTimer);
            }
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

        /*
        // ▼▼▼ 원본 코드 (2명 상호작용) ▼▼▼
        if (!interactingPlayerIDs.Contains(playerActorNumber))
        {
            interactingPlayerIDs.Add(playerActorNumber);
        }
        if (interactingPlayerIDs.Count >= 2)
        {
            pv.RPC("RPC_ActivateDevice", RpcTarget.AllBuffered);
        }
        */

        // ▼▼▼ 수정된 코드 (1명 상호작용) ▼▼▼
        if (!interactingPlayerIDs.Contains(playerActorNumber))
        {
            interactingPlayerIDs.Add(playerActorNumber);
        }
        // 한 명이라도 상호작용하면 바로 장치를 활성화합니다.
        pv.RPC("RPC_ActivateDevice", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void RPC_ActivateDevice()
    {
        if (isActivated) return;
        isActivated = true;
        deactivationTimer = timeToDeactivate;
        networkDeactivationTimer = timeToDeactivate;

        if (activationVisual != null) activationVisual.SetActive(true);
        if (purifierUICanvas != null)
        {
            purifierUICanvas.SetActive(true);
            UpdateSlider(deactivationTimer);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ActiveStateRoutine());
        }
    }

    private IEnumerator ActiveStateRoutine()
    {
        if (initialSpawnMonsters != null && initialSpawnMonsters.Count > 0 && initialSpawnCount > 0)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                // 리스트에서 랜덤 몬스터 선택
                GameObject monsterPrefab = initialSpawnMonsters[Random.Range(0, initialSpawnMonsters.Count)];
                SpawnMonster(monsterPrefab);
                yield return new WaitForSeconds(0.1f); // 몬스터가 겹치지 않게 약간의 딜레이
            }
        }

        float spawnTimer = spawnInterval;
        while (isActivated)
        {
            if (spawnTimer <= 0f)
            {
                if (continuousMonsterPrefabs != null && continuousMonsterPrefabs.Count > 0)
                {
                    GameObject monsterToSpawn = continuousMonsterPrefabs[Random.Range(0, continuousMonsterPrefabs.Count)];
                    SpawnMonster(monsterToSpawn);
                }
                spawnTimer = spawnInterval;     
            }   
            else
            {
                spawnTimer -= Time.deltaTime;
            }

            /*
            // ▼▼▼ 원본 코드 (2명 상호작용 시) ▼▼▼
            if (interactingPlayerIDs.Count < 2)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            GameObject player1Obj = FindPlayerObject(GetPlayerByActorNumber(interactingPlayerIDs[0]));
            GameObject player2Obj = FindPlayerObject(GetPlayerByActorNumber(interactingPlayerIDs[1]));

            if (player1Obj == null || player2Obj == null ||
                Vector3.Distance(transform.position, player1Obj.transform.position) > maxPlayerDistance ||
                Vector3.Distance(transform.position, player2Obj.transform.position) > maxPlayerDistance)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            if (Vector3.Distance(player1Obj.transform.position, player2Obj.transform.position) <= deactivationRange)
            {
                deactivationTimer -= Time.deltaTime;
            }
            else
            {
                deactivationTimer = timeToDeactivate;
            }
            */

            // ▼▼▼ 수정된 코드 (1명 이상 상호작용 시) ▼▼▼
            if (interactingPlayerIDs.Count < 1) // 상호작용한 플레이어가 한 명도 없으면 초기화
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            // 상호작용한 모든 플레이어가 장치 범위를 벗어났는지 확인
            bool allPlayersInRange = false;
            foreach (int id in interactingPlayerIDs)
            {
                GameObject playerObj = FindPlayerObject(GetPlayerByActorNumber(id));
                if (playerObj != null && Vector3.Distance(transform.position, playerObj.transform.position) <= maxPlayerDistance)
                {
                    allPlayersInRange = true;
                    break;
                }
            }

            // 모든 플레이어가 범위를 벗어나면 초기화
            if (!allPlayersInRange)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            // 한 명이라도 범위 내에 있으면 정화 타이머 감소
            deactivationTimer -= Time.deltaTime;

            UpdateSlider(deactivationTimer);



            if (deactivationTimer <= 0)
            {
                
                pv.RPC("RPC_CompletePurification", RpcTarget.AllBuffered);
                yield break;
            }

            yield return null;
        }
    }

    private void SpawnMonster(GameObject monsterPrefab)
    {
        if (monsterPrefab == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * maxPlayerDistance;
        randomDirection += transform.position;
        Vector3 Yoffset = new Vector3(0, 1.5f, 0);
        randomDirection.y = transform.position.y;

        NavMeshHit hit;
        Vector3 finalPosition = transform.position;

        if (NavMesh.SamplePosition(randomDirection, out hit, maxPlayerDistance, NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }
        else
        {
            Debug.LogWarning("몬스터를 소환할 유효한 NavMesh 위치를 찾지 못했습니다. 기본 위치에 소환합니다.");
        }

        GameObject monster = PhotonNetwork.Instantiate(resourcePath + monsterPrefab.name, finalPosition + Yoffset, Quaternion.identity);
        spawnedMonsters.Add(monster);
    }

    [PunRPC]
    private void RPC_ResetDevice()
    {
        isActivated = false;
        if (activationVisual != null) activationVisual.SetActive(false);
        if (purifierUICanvas != null) purifierUICanvas.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            StopAllCoroutines();
            interactingPlayerIDs.Clear();
            
            
            spawnedMonsters.Clear();
        }
    }
    [PunRPC]
    private void RPC_CompletePurification()
    {
        if (isCompleted) return; // 이미 완료되었으면 중복 실행 방지

        isCompleted = true;
        isActivated = false;

        // 시각/UI 효과 끄기 (또는 완료된 상태의 비주얼로 변경 가능)
        if (activationVisual != null) activationVisual.SetActive(false);
        if (purifierUICanvas != null) purifierUICanvas.SetActive(false);

        // 마스터 클라이언트가 매니저에게 알리기
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log(gameObject.name + " 정화 완료! PurifierManager에 알림.");

            // 매니저에게 알림
            if (PurifierManager.Instance != null)
            {
                PurifierManager.Instance.GetComponent<PhotonView>().RPC("NotifyPurifierCompleted", RpcTarget.MasterClient);
            }
            else
            {
                
                Debug.Log("PurifierManager가 없습니다.");
            }
            if (objectToDestroy != null)
            {
                Destroy(objectToDestroy);
                Debug.Log("PurifierManager에 배정되지 않은 오브젝트. 길을 엽니다");
            }
            StopAllCoroutines();
        }
        gameObject.SetActive(false); // 정화 완료 후 오브젝트 비활성화
    }

    private void UpdateSlider(float currentTime)
    {
        if (progressSlider != null)
        {
            progressSlider.value = (timeToDeactivate - currentTime) / timeToDeactivate;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(deactivationTimer);
        }
        else
        {
            this.networkDeactivationTimer = (float)stream.ReceiveNext();
        }
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deactivationRange);

        Gizmos.color = new Color(0, 0, 1, 0.5f);
        Gizmos.DrawSphere(transform.position, maxPlayerDistance);
    }
#endif
}