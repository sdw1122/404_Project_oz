using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Woodman 보스전의 전체적인 흐름을 관리하는 매니저 스크립트입니다.
/// 몬스터 소환을 제어하고, 보스 사망 시 모든 몬스터를 제거하는 역할을 합니다.
/// </summary>
public class WoodmanBossManager : MonoBehaviourPunCallbacks
{
    [Header("보스 및 스포너 설정")]
    [Tooltip("씬에 있는 WoodMan 보스를 연결하세요.")]
    [SerializeField] private WoodMan woodmanBoss;

    [Tooltip("몬스터를 소환할 WoodManMonsterSpawner를 연결하세요.")]
    [SerializeField] private WoodManMonsterSpawner monsterSpawner;

    [Header("소환 설정")]
    [Tooltip("몬스터 소환 간격 (초)")]
    [SerializeField] private float spawnInterval = 5f;

    // --- 내부 변수 ---
    private Coroutine spawnCoroutine;
    private bool isBossDead = false;

    void Start()
    {
        // 마스터 클라이언트만 보스전 로직을 시작합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            if (woodmanBoss == null || monsterSpawner == null)
            {
                Debug.LogError("WoodmanBossManager에 보스 또는 몬스터 스포너가 연결되지 않았습니다!");
                return;
            }

            // 몬스터 소환 시작
            spawnCoroutine = StartCoroutine(SpawnMonstersRoutine());
        }
    }

    void Update()
    {
        // 마스터 클라이언트만 보스의 상태를 감시합니다.
        if (!PhotonNetwork.IsMasterClient || isBossDead)
        {
            return;
        }

        // 보스가 죽었는지 확인합니다.
        if (woodmanBoss != null && woodmanBoss.dead)
        {
            isBossDead = true;
            Debug.Log("Woodman 보스가 사망했습니다. 몬스터 소환을 중지하고 남은 몬스터를 제거합니다.");

            // 1. 몬스터 소환 중지
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            // 2. 남아있는 모든 몬스터 제거
            monsterSpawner.DestroyAllSpawnedMonsters();
        }
    }

    /// <summary>
    /// 일정 간격으로 몬스터를 계속 소환하는 코루틴입니다.
    /// </summary>
    private IEnumerator SpawnMonstersRoutine()
    {
        while (!isBossDead)
        {
            monsterSpawner.CreateEnemy(); // 스포너에게 몬스터 생성을 요청
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
