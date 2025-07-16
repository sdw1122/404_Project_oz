using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Transform))]
public class ArrowFxTest : MonoBehaviour
{
        [Tooltip("이동할 방향 (Unit Vector 권장)")]
        public Vector3 moveDirection = Vector3.forward;

        [Tooltip("이동 속도 (유닛/초)")]
        public float speed = 5f;

    void Update()
    {
        // 방향 벡터를 정규화해 주면, moveDirection 값의 크기와 무관하게
        // speed 값만큼만 이동하게 됩니다.
        Vector3 dir = moveDirection.normalized;

        // Time.deltaTime을 곱해 프레임독립적(초당) 이동
        transform.position += dir * speed * Time.deltaTime;
    }
}
