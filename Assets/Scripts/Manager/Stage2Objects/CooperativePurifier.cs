using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.AI;

/// <summary>
/// [몬스터 스폰 반경 통합 버전] 두 명의 플레이어가 협력하여 활성화하는 장치입니다.
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
    [Tooltip("소환할 몬스터 프리팹 리스트 ('Resources/Model/Prefab/Stage2/' 경로 안에 있어야 함)")]
    [SerializeField] private List<GameObject> monsterPrefabs;
    [SerializeField] private float spawnInterval = 5f;

    private string resourcePath = "Model/Prefab/Stage2/";
    private bool isActivated = false;
    private float deactivationTimer = 0f;
    private List<int> interactingPlayerIDs = new List<int>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    private float networkDeactivationTimer = 0f;

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
        float spawnTimer = spawnInterval;
        while (isActivated)
        {
            if (spawnTimer <= 0f)
            {
                SpawnMonster();
                spawnTimer = spawnInterval;
            }
            else
            {
                spawnTimer -= Time.deltaTime;
            }

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

            UpdateSlider(deactivationTimer);

            if (deactivationTimer <= 0)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break;
            }

            yield return null;
        }
    }

    // ----- ▼▼▼ [수정된 부분] 몬스터 소환 로직 ▼▼▼ -----
    private void SpawnMonster()
    {
        if (monsterPrefabs == null || monsterPrefabs.Count == 0) return;

        GameObject monsterToSpawn = monsterPrefabs[Random.Range(0, monsterPrefabs.Count)];
        if (monsterToSpawn == null) return;

        // maxPlayerDistance를 스폰 반경으로 사용
        Vector3 randomDirection = Random.insideUnitSphere * maxPlayerDistance;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;

        NavMeshHit hit;
        Vector3 finalPosition = transform.position;

        // NavMesh 위에서 유효한 위치를 찾음
        if (NavMesh.SamplePosition(randomDirection, out hit, maxPlayerDistance, NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }
        else
        {
            Debug.LogWarning("몬스터를 소환할 유효한 NavMesh 위치를 찾지 못했습니다. 기본 위치에 소환합니다.");
        }

        GameObject monster = PhotonNetwork.Instantiate(resourcePath + monsterToSpawn.name, finalPosition, Quaternion.identity);
        spawnedMonsters.Add(monster);
    }
    // ----- ▲▲▲ [수정된 부분] ▲▲▲ -----

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
            foreach (var monster in spawnedMonsters)
            {
                if (monster != null) PhotonNetwork.Destroy(monster);
            }
            spawnedMonsters.Clear();
        }
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
        // deactivationRange 범위를 노란색 와이어 스피어로 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deactivationRange);

        // ----- ▼▼▼ [수정된 부분] 플레이어 이탈 및 몬스터 스폰 반경 시각화 ▼▼▼ -----
        Gizmos.color = new Color(0, 0, 1, 0.5f); // 파란색 반투명
        Gizmos.DrawSphere(transform.position, maxPlayerDistance);
        // ----- ▲▲▲ [수정된 부분] ▲▲▲ -----
    }
#endif
}