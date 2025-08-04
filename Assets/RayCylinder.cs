using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class RayCylinder : MonoBehaviour
{
    [Header("Ray 설정")]
    public Transform startPoint;        // 레이 시작 지점
    public Vector3 direction = Vector3.forward; // 로컬 forward 기준
    public float maxDistance = 100f;
    public LayerMask layerMask = ~0;

    [Header("Cylinder 설정")]
    public GameObject cylinderPrefab;   // 씬에 배치할 Cylinder 프리팹 (높이 2, 중심이 로컬 원점)
    public float radius = 0.1f;         // 실린더 반지름

    private GameObject _cylinder;
    private Transform _cylT;

    void Start()
    {
        if (cylinderPrefab == null)
        {
            Debug.LogError("cylinderPrefab을 인스펙터에 할당하세요.");
            enabled = false;
            return;
        }
        // 인스턴스 생성
        _cylinder = Instantiate(cylinderPrefab, Vector3.zero, Quaternion.identity);
        _cylT = _cylinder.transform;

        // 기본 스케일 세팅 (높이 2인 Unity 원통)
        _cylT.localScale = new Vector3(radius * 2, 1f, radius * 2);
    }

    void Update()
    {
        // 월드 공간에서의 레이 방향 계산
        Vector3 dir = startPoint.TransformDirection(direction.normalized);

        // 레이 캐스트
        if (Physics.Raycast(startPoint.position, dir, out RaycastHit hit, maxDistance, layerMask))
        {
            float dist = hit.distance;

            // 1) 위치: start → hit 중간
            Vector3 mid = startPoint.position + dir * (dist * 0.5f);
            _cylT.position = mid;

            // 2) 회전: 실린더 로컬 Y축이 dir을 향하도록
            _cylT.rotation = Quaternion.LookRotation(dir, Vector3.up)
                             * Quaternion.Euler(90f, 0f, 0f);
            //    ↑ Unity 기본 Cylinder가 Z축이 위쪽이므로 X축 기준 90° 회전

            // 3) 스케일: 높이 방향(Y)을 dist/2로 조절
            //    원통 기본 높이 = 2 → localScale.y * 2 = 실제 높이
            Vector3 s = _cylT.localScale;
            s.y = dist * 0.5f;
            _cylT.localScale = s;

            _cylinder.SetActive(true);
        }
        else
        {
            StartCoroutine(WaitHalfSeconds());
            // 히트 없으면 숨김
            _cylinder.SetActive(false);
        }
    }
    IEnumerator WaitHalfSeconds()
    {
        yield return new WaitForSeconds(0.5f);
    }
}
