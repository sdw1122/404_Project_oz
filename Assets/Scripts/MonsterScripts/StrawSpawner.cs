using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
[System.Serializable]
public class SpawnGroup
{
    public GameObject enemyPrefab; // 생성할 몬스터 프리팹
    public int count;              // 몇 마리 생성할지
}
public class StrawSpawner : MonoBehaviour
{
    public List<SpawnGroup> wave;
    public Transform[] spawnPoints;
    public string resourcePath = "Model/Prefab/Stage2/";
    public float spawnDelay = 0.5f;
    private List<Enemy> enemyList = new List<Enemy>();
    private bool isSpawning = false; 

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (enemyList.Count <= 0 && !isSpawning)
        {
            StartCoroutine(SpawnWaveRoutine());
        }

    }
    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        Debug.Log("새로운 웨이브를 시작합니다.");

        // wave 순서대로 생성하자.
        foreach (SpawnGroup group in wave)
        {
            // count 만큼 생성
            for (int i = 0; i < group.count; i++)
            {
                CreateEnemy(group.enemyPrefab);
                // 다음 몬스터 생성 전 잠시 대기
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        isSpawning = false;
    }
    private void CreateEnemy(GameObject enemyPrefab)
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemyObj = PhotonNetwork.Instantiate(resourcePath + enemyPrefab.name, spawnPoint.position, spawnPoint.rotation);


        Enemy enemy = enemyObj.GetComponent<Enemy>();
       

        if (PhotonNetwork.IsMasterClient)

        {

            enemyList.Add(enemy);

            enemy.onDeath += () => enemyList.Remove(enemy);
            enemy.onDeath += () => Destroy(enemy.gameObject);
        }
    }

}
