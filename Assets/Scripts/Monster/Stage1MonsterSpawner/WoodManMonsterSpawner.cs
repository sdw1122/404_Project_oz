using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// Woodman 보스전에서 몬스터를 소환하는 역할을 합니다.
/// WoodmanBossManager의 제어를 받습니다.
/// </summary>
public class WoodManMonsterSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹 및 데이터")]
    [Tooltip("소환할 몬스터 프리팹 리스트 ('Resources' 폴더 내 경로에 있어야 함)")]
    public List<GameObject> enemyPrefabs;
    [Tooltip("몬스터에게 적용할 데이터 (선택 사항)")]
    public EnemyData[] enemyDatas;

    [Header("소환 위치")]
    [Tooltip("몬스터가 소환될 위치들")]
    public Transform[] spawnPoints;
    private string resourcePath = "Model/Prefab/Stage1/"; // 프리팹 경로

    // 생성된 몬스터를 추적하기 위한 리스트
    private List<Enemy> spawnedEnemies = new List<Enemy>();

    /// <summary>
    /// 몬스터 한 마리를 생성하고 추적 리스트에 추가합니다.
    /// </summary>
    public void CreateEnemy()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("몬스터 프리팹 또는 스폰 위치가 설정되지 않았습니다.");
            return;
        }

        // 랜덤 몬스터와 랜덤 위치 선택
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

            // 생성된 몬스터를 리스트에 추가
            spawnedEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// 현재까지 소환된 모든 몬스터를 파괴합니다.
    /// </summary>
    public void DestroyAllSpawnedMonsters()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 리스트를 복사하여 순회 (원본 리스트 변경에 따른 문제 방지)
        List<Enemy> enemiesToDestroy = new List<Enemy>(spawnedEnemies);

        foreach (Enemy enemy in enemiesToDestroy)
        {
            if (enemy != null && !enemy.dead)
            {
                // 네트워크를 통해 몬스터 오브젝트 파괴
                PhotonNetwork.Destroy(enemy.gameObject);
            }
        }
        // 추적 리스트 초기화
        spawnedEnemies.Clear();
    }
}