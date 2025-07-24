using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> enemyPrefabs; // 몬스터 프리팹 리스트
    public EnemyData[] enemyDatas;
    public Transform[] spawnPoints;
    string resourcePath = "Model/Prefab/";    

    private List<Enemy> enemyList = new List<Enemy>();
    private int wave;

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
        /*wave++;
        int spawnCount = Mathf.RoundToInt(wave * 1.5f);
        for (int i = 0; i < spawnCount; i++)
        {
            CreateEnemy();
        }*/
        CreateEnemy();
    }
    private void CreateEnemy()
    {
        Debug.Log("CreateEnemy 메서드 실행");
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        EnemyData enemyData = enemyDatas[Random.Range(0, enemyDatas.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // 포톤 인스턴스 생성
        GameObject enemyObj = PhotonNetwork.Instantiate(resourcePath + enemyPrefab.name, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"{ enemyObj.name}" );
        if (enemyObj == null)
        {
           
            return;
        }
        // Enemy 컴포넌트 가져오기
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
          
            return;
        }

     

        // 무슨적이 나올지는 "마스터 클라이언트만" 수행
        if (PhotonNetwork.IsMasterClient)
        {
           
            enemy.Setup(enemyData);
            enemyList.Add(enemy);
            enemy.onDeath += () => enemyList.Remove(enemy);
            enemy.onDeath += () => PhotonNetwork.Destroy(enemy.gameObject);
        }        
    }
    
}
