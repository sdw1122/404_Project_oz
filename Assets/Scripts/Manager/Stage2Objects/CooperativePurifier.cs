using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Linq 사용을 위해 추가
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.AI;

[RequireComponent(typeof(PhotonView))]
public class CooperativePurifier : InteractableBase, IPunObservable
{
    [Header("UI 설정")]
    [SerializeField] private GameObject purifierUICanvas;
    [SerializeField] private Slider progressSlider;
    private Camera localPlayerCamera;

    [Header("활성화/비활성화 대상")]
    [SerializeField] private GameObject activationVisual;

    [Header("정화(비활성화) 설정")]
    [Tooltip("이 범위 안에 2명의 플레이어가 있어야 정화가 진행됩니다.")]
    [SerializeField] private float deactivationRange = 5f; // [이름 변경] 상호작용 범위로 의미 명확화
    [SerializeField] private float timeToDeactivate = 10f;

    [Header("초기화 및 스폰 반경 설정")]
    [SerializeField] private float maxPlayerDistance = 30f;

    [Header("몬스터 소환 설정")]
    [SerializeField] private List<GameObject> initialSpawnMonsters;
    [SerializeField] private int initialSpawnCount;
    [SerializeField] private List<GameObject> continuousMonsterPrefabs;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private GameObject objectToDestroy;

    private string resourcePath = "Model/Prefab/Stage2/";
    private bool isActivated = false;
    private float deactivationTimer = 0f;

    // [수정] 상호작용한 플레이어가 아닌, 장치 근처의 모든 플레이어를 추적
    private List<PlayerController> playersNearDevice = new List<PlayerController>();
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    private float networkDeactivationTimer = 0f;
    private bool isCompleted = false;

    void Start()
    {
        if (activationVisual != null) activationVisual.SetActive(false);
        if (purifierUICanvas != null) purifierUICanvas.SetActive(false);
    }

    private void Update()
    {
        if (isActivated && purifierUICanvas != null)
        {
            if (localPlayerCamera == null || !localPlayerCamera.gameObject.activeInHierarchy)
            {
                // 로컬 플레이어 카메라를 찾는 더 안정적인 방법
                foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
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

            // 마스터 클라이언트가 보낸 타이머 값으로 슬라이더 업데이트
            if (!PhotonNetwork.IsMasterClient)
            {
                UpdateSlider(networkDeactivationTimer);
            }
        }
    }

    // [수정] 이제 상호작용은 단순히 장치를 활성화시키는 트리거 역할만 합니다.
    public override void Interact(PlayerController player)
    {
        if (isActivated) return;
        // 한 명이라도 상호작용하면 모든 클라이언트에게 장치 활성화를 요청
        pv.RPC("RPC_ActivateDevice", RpcTarget.AllBuffered);
    }

    // [삭제] RPC_RegisterInteraction 함수는 더 이상 필요 없으므로 삭제합니다.

    [PunRPC]
    private void RPC_ActivateDevice()
    {
        if (isActivated) return;
        isActivated = true;
        deactivationTimer = 0f; // 타이머를 0에서 시작해서 채워나가는 방식으로 변경 (더 직관적)
        networkDeactivationTimer = 0f;

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
        // 초기 몬스터 소환
        if (initialSpawnMonsters != null && initialSpawnMonsters.Count > 0 && initialSpawnCount > 0)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                GameObject monsterPrefab = initialSpawnMonsters[Random.Range(0, initialSpawnMonsters.Count)];
                SpawnMonster(monsterPrefab);
                yield return new WaitForSeconds(0.3f);
            }
        }

        float spawnTimer = spawnInterval;
        while (isActivated)
        {
            // --- 지속 몬스터 소환 로직 (기존과 동일) ---
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                if (continuousMonsterPrefabs != null && continuousMonsterPrefabs.Count > 0)
                {
                    GameObject monsterToSpawn = continuousMonsterPrefabs[Random.Range(0, continuousMonsterPrefabs.Count)];
                    SpawnMonster(monsterToSpawn);
                }
                spawnTimer = spawnInterval;
            }

            // ================== [핵심 로직 수정] ==================
            // 1. 장치 주변의 모든 플레이어를 찾습니다.
            playersNearDevice = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                .Where(p => !p.GetComponent<PlayerHealth>().dead && Vector3.Distance(transform.position, p.transform.position) <= maxPlayerDistance)
                .ToList();

            // 2. 장치 근처에 플레이어가 아무도 없으면 초기화합니다.
            if (playersNearDevice.Count == 0)
            {
                pv.RPC("RPC_ResetDevice", RpcTarget.AllBuffered);
                yield break; // 코루틴 중단
            }

            // 3. 정화 범위(deactivationRange) 안에 있는 플레이어 수를 셉니다.
            int playersInDeactivationZone = playersNearDevice
                .Count(p => Vector3.Distance(transform.position, p.transform.position) <= deactivationRange);

            // 4. 범위 안에 2명 이상 있으면 타이머를 진행시키고, 아니면 타이머를 되돌립니다.
            if (playersInDeactivationZone >= 2)
            {
                deactivationTimer += Time.deltaTime;
            }
            else
            {
                // 서서히 감소하도록 하여 플레이 경험을 개선
                deactivationTimer -= Time.deltaTime * 0.5f;
                if (deactivationTimer < 0) deactivationTimer = 0;
            }
            // ======================================================

            // 모든 클라이언트에 타이머 상태 업데이트
            UpdateSlider(deactivationTimer);

            // 정화 완료 조건 확인
            if (deactivationTimer >= timeToDeactivate)
            {
                pv.RPC("RPC_CompletePurification", RpcTarget.AllBuffered);
                yield break; // 코루틴 중단
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

            // --- [수정] 씬에 남아있는 몬스터들을 실제로 파괴 ---
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

    [PunRPC]
    private void RPC_CompletePurification()
    {
        if (isCompleted) return;
        isCompleted = true;
        isActivated = false;

        if (activationVisual != null) activationVisual.SetActive(false);
        if (purifierUICanvas != null) purifierUICanvas.SetActive(false);

        if (objectToDestroy != null)
        {
            objectToDestroy.SetActive(false);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // --- [수정] 완료 시에도 소환된 몬스터는 정리해주는 것이 깔끔함 ---
            foreach (var monster in spawnedMonsters)
            {
                if (monster != null)
                {
                    PhotonNetwork.Destroy(monster);
                }
            }
            spawnedMonsters.Clear();

            if (PurifierManager.Instance != null)
            {
                PurifierManager.Instance.GetComponent<PhotonView>().RPC("NotifyPurifierCompleted", RpcTarget.MasterClient);
            }
            
            StopAllCoroutines();
        }
        gameObject.SetActive(false);
    }

    private void UpdateSlider(float currentTime)
    {
        if (progressSlider != null)
        {
            // [수정] 타이머가 0에서 시작하므로 value 계산 방식 변경
            progressSlider.value = currentTime / timeToDeactivate;
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

    // [삭제] GetPlayerByActorNumber, FindPlayerObject 함수는 더 이상 필요 없으므로 삭제합니다.

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deactivationRange);
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawSphere(transform.position, maxPlayerDistance);
    }
#endif
}