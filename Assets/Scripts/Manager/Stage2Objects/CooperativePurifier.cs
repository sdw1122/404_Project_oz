using UnityEngine;
using UnityEngine.UI; // UI 관련 네임스페이스 추가
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// [자체 UI 관리 버전] 두 명의 플레이어가 협력하여 활성화하는 장치입니다.
/// 자식 오브젝트로 포함된 World Space UI를 직접 제어하여 진행률을 표시합니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class CooperativePurifier : InteractableBase, IPunObservable
{
    [Header("UI 설정")]
    [Tooltip("정화 장치의 자식으로 있는 UI Canvas 오브젝트를 연결하세요.")]
    [SerializeField] private GameObject purifierUICanvas;
    [Tooltip("Canvas 안에 있는 Slider를 연결하세요.")]
    [SerializeField] private Slider progressSlider;
    private Camera localPlayerCamera; // 각 클라이언트의 카메라

    [Header("활성화/비활성화 대상")]
    [SerializeField] private GameObject activationVisual;

    [Header("정화(비활성화) 설정")]
    [SerializeField] private float deactivationRange = 5f;
    [SerializeField] private float timeToDeactivate = 10f;

    [Header("초기화 설정")]
    [SerializeField] private float maxPlayerDistance = 30f;

    private string resourcePath = "Model/Prefab/Stage2/";
    [Header("몬스터 소환 설정")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;

    private bool isActivated = false;
    private float deactivationTimer = 0f;
    private List<int> interactingPlayerIDs = new List<int>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    // 네트워크 동기화를 위한 변수
    private float networkDeactivationTimer = 0f;

    void Start()
    {
        if (activationVisual != null)
        {
            activationVisual.SetActive(false);
        }
        // 시작 시 UI를 비활성화합니다.
        if (purifierUICanvas != null)
        {
            purifierUICanvas.SetActive(false);
        }
    }

    private void Update()
    {
        // 장치가 활성화되었을 때만 UI 로직을 처리합니다.
        if (isActivated && purifierUICanvas != null)
        {
            // 모든 클라이언트에서 UI가 자신의 카메라를 바라보게 합니다.
            if (localPlayerCamera == null)
            {
                // PlayerController 스크립트를 가진 로컬 플레이어를 찾아 카메라를 가져옵니다.
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
                purifierUICanvas.transform.LookAt(purifierUICanvas.transform.position + localPlayerCamera.transform.rotation * Vector3.forward,
                    localPlayerCamera.transform.rotation * Vector3.up);
            }

            // 마스터 클라이언트가 아닌 경우, 네트워크로 받은 타이머 값을 슬라이더에 반영합니다.
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
        if (!interactingPlayerIDs.Contains(playerActorNumber))
        {
            interactingPlayerIDs.Add(playerActorNumber);
        }
        if (interactingPlayerIDs.Count >= 2)
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
        networkDeactivationTimer = timeToDeactivate; // 초기값 동기화

        if (activationVisual != null)
        {
            activationVisual.SetActive(true);
        }

        // 모든 클라이언트에서 UI를 활성화합니다.
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
        float spawnTimer = spawnInterval;
        while (isActivated)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnMonster();
                spawnTimer = spawnInterval;
            }

            if (interactingPlayerIDs.Count < 2)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            Player player1 = GetPlayerByActorNumber(interactingPlayerIDs[0]);
            Player player2 = GetPlayerByActorNumber(interactingPlayerIDs[1]);
            GameObject player1Obj = FindPlayerObject(player1);
            GameObject player2Obj = FindPlayerObject(player2);

            if (player1Obj == null || player2Obj == null ||
                Vector3.Distance(transform.position, player1Obj.transform.position) > maxPlayerDistance ||
                Vector3.Distance(transform.position, player2Obj.transform.position) > maxPlayerDistance)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            bool arePlayersCloseEnough = Vector3.Distance(player1Obj.transform.position, player2Obj.transform.position) <= deactivationRange;

            if (arePlayersCloseEnough)
            {
                deactivationTimer -= Time.deltaTime;
            }
            else
            {
                deactivationTimer = timeToDeactivate;
            }
            

            // 마스터 클라이언트는 직접 UI를 업데이트합니다.
            UpdateSlider(deactivationTimer);

            if (deactivationTimer <= 0)
            {
                
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

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
        // 모든 클라이언트에서 UI를 비활성화합니다.
        if (purifierUICanvas != null)
        {
            purifierUICanvas.SetActive(false);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            StopAllCoroutines();
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

    /// <summary>
    /// 슬라이더의 값을 업데이트하는 로컬 함수입니다.
    /// </summary>
    private void UpdateSlider(float currentTime)
    {
        if (progressSlider != null)
        {
            // 시간이 차오르는 것처럼 보이게 (최대시간 - 남은시간)으로 계산합니다.
            progressSlider.value = (timeToDeactivate - currentTime) / timeToDeactivate;
        }
    }

    /// <summary>
    /// deactivationTimer 값을 네트워크를 통해 동기화합니다.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터 클라이언트가 자신의 deactivationTimer 값을 다른 클라이언트에게 보냅니다.
            stream.SendNext(deactivationTimer);
        }
        else
        {
            // 다른 클라이언트들은 마스터로부터 deactivationTimer 값을 받습니다.
            this.networkDeactivationTimer = (float)stream.ReceiveNext();
        }
    }

    // --- 아래는 기존과 동일한 함수들 ---
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // maxPlayerDistance 범위를 파란색 와이어 스피어로 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxPlayerDistance);

        // deactivationRange 범위를 노란색 와이어 스피어로 표시 (현재 로직에서 미사용)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deactivationRange);

        // 몬스터 스폰 위치를 빨간색 구로 표시
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.red;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.5f); // 0.5f 크기의 구로 표시
                }
            }
        }
    }
#endif
}