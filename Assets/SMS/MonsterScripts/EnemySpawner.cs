using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class EnemySpawner : MonoBehaviour
{
    public Enemy enemyPrefab;
    public EnemyData[] enemyDatas;
    public Transform[] spawnPoints;
    
    private List<Enemy> enemyList = new List<Enemy>();
    private int wave;
    private void Awake()
    {
       
    }
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

            if (enemyList.Count <= 0)
        {
            SpawnWave();
        }
    }
    private void SpawnWave()
    {
        wave++;
        int spawnCount = Mathf.RoundToInt(wave * 1.5f);
        for (int i = 0; i < spawnCount; i++)
        {
            CreateEnemy();
        }
    }
    private void CreateEnemy()
    {
        EnemyData enemyData = enemyDatas[Random.Range(0, enemyDatas.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // 포톤 인스턴스 생성
        GameObject enemyObj = PhotonNetwork.Instantiate("enemyPrefab", spawnPoint.position, spawnPoint.rotation);

        // Enemy 컴포넌트 가져오기
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        // 무슨적이 나올지는 "마스터 클라이언트만" 수행
        if (PhotonNetwork.IsMasterClient)
        {
            enemy.Setup(enemyData);
        }

        // 적 리스트 관리도 마스터만 , 둘이 동시에 시행된다면 꼬일 수 있다.
        if (PhotonNetwork.IsMasterClient)
        {
            enemyList.Add(enemy);
            enemy.onDeath += () => enemyList.Remove(enemy);
            enemy.onDeath += () => PhotonNetwork.Destroy(enemy.gameObject);
        }



    }
    
}
