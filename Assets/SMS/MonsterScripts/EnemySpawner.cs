using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public Enemy enemyPrefab;
    public EnemyData[] enemyDatas;
    public Transform[] spawnPoints;

    private List<Enemy> enemyList = new List<Enemy>();
    private int wave;
    private void Update()
    {
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
        Enemy enemy= Instantiate(enemyPrefab, spawnPoint.position,spawnPoint.rotation);
        enemy.Setup(enemyData);
        enemyList.Add(enemy);
        enemy.onDeath += () => enemyList.Remove(enemy);
        enemy.onDeath += () => Destroy(enemy.gameObject, 2f);

       
    }

}
