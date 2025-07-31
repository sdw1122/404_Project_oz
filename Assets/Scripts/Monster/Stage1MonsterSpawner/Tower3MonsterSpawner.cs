using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// 레버에 의해 활성화되어 몬스터를 순차적으로 소환하고,
/// 소환된 몬스터의 상태를 직접 감시하여 모두 죽으면 MovingObject를 작동시키는 스크립트입니다.
/// </summary>
public class Tower3MonsterSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹 및 데이터")]
    [Tooltip("소환할 몬스터 프리팹 리스트 ('Resources/Model/Prefab/Stage1/' 경로 안에 있어야 함)")]
    public List<GameObject> enemyPrefabs;
    [Tooltip("몬스터에게 적용할 데이터 (선택 사항)")]
    public EnemyData[] enemyDatas;

    [Header("소환 위치 및 경로")]
    [Tooltip("몬스터가 소환될 위치들")]
    public Transform[] spawnPoints;
    private string resourcePath = "Model/Prefab/Stage1/";

    [Header("소환 규칙 설정")]
    [Tooltip("이 스포너가 활성화되었을 때 소환할 몬스터의 총 수량")]
    [SerializeField] private int totalSpawnCount = 10;
    [Tooltip("각 몬스터가 소환될 때의 시간 간격(초)")]
    [SerializeField] private float spawnInterval = 2.0f;

    [Header("연동 오브젝트 설정")]
    [Tooltip("소환된 몬스터가 모두 죽으면 작동시킬 MovingObject 입니다. (선택 사항)")]
    [SerializeField] private MovingObject targetMovingObject;

    // --- 내부 변수 ---
    private List<Enemy> spawnedEnemies = new List<Enemy>();
    private bool hasBeenActivated = false;
    private bool isSpawningFinished = false; // 몬스터 소환이 모두 완료되었는지 확인하는 플래그

    private void Update()
    {
        // 이 로직은 마스터 클라이언트만, 그리고 몬스터 소환이 모두 끝난 후에만 실행됩니다.
        if (!PhotonNetwork.IsMasterClient || !isSpawningFinished || spawnedEnemies.Count == 0)
        {
            return;
        }

        // ▼▼▼ [핵심 변경점] 몬스터 리스트를 직접 순회하며 죽었는지 검사합니다. ▼▼▼
        // 리스트를 뒤에서부터 순회해야 순회 중 제거해도 오류가 발생하지 않습니다.
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            // 리스트에 있는 몬스터가 파괴되었거나(null), 죽었다면(dead) 리스트에서 제거합니다.
            if (spawnedEnemies[i] == null || spawnedEnemies[i].dead)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }
        // ▲▲▲▲▲

        // 모든 검사가 끝난 후, 리스트가 비어있다면 (모든 몬스터가 죽었다면)
        if (spawnedEnemies.Count == 0)
        {
            Debug.Log("모든 몬스터가 처치된 것을 감지! MovingObject를 작동시킵니다.");
            isSpawningFinished = false; // 중복 실행을 막기 위해 상태를 다시 되돌림

            if (targetMovingObject != null)
            {
                targetMovingObject.GetComponent<PhotonView>().RPC("ToggleMoveState", RpcTarget.All);
            }
        }
    }

    /// <summary>
    /// 외부에서 호출하여 몬스터 소환 코루틴을 시작하는 메인 함수입니다.
    /// </summary>
    public void ActivateSpawner()
    {
        if (!PhotonNetwork.IsMasterClient || hasBeenActivated) return;
        hasBeenActivated = true;

        Debug.Log("몬스터 스포너 활성화! " + totalSpawnCount + "마리를 " + spawnInterval + "초 간격으로 소환합니다.");
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < totalSpawnCount; i++)
        {
            CreateEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log("모든 몬스터 소환 완료. 이제부터 몬스터 처치 여부를 감시합니다.");
        isSpawningFinished = true; // 소환이 모두 끝났음을 표시
    }

    private void CreateEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || spawnPoints == null || spawnPoints.Length == 0) return;

        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyObj = PhotonNetwork.Instantiate(resourcePath + enemyPrefab.name, spawnPoint.position, spawnPoint.rotation);

        if (enemyObj == null) return;

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            if (enemyDatas != null && enemyDatas.Length > 0)
            {
                enemy.Setup(enemyDatas[Random.Range(0, enemyDatas.Length)]);
            }

            // 생성된 몬스터를 리스트에 추가 (이벤트 구독 로직은 제거)
            spawnedEnemies.Add(enemy);
        }
    }
}