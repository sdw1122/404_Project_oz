using UnityEngine;

public class EnemyState : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public interface IEnemyState
    {
        void OnEnter(Enemy enemy);
        void UpdateState(Enemy enemy);
        void OnExit(Enemy enemy);
    }
}
