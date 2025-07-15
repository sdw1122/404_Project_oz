using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float Max_HP = 100f; // 체력
    public float Atk_Damage = 20f; // 공격력
    public float Atk_Cooldown = 2.0f; // 공격 간격
    public float DEF_Factor = 1f; // 받는 피해량 계수
    public float speed = 2f; // 이동 속도
   
    public Color skinColor = Color.white; // 피부색
}
