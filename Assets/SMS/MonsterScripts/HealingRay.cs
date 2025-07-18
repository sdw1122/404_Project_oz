using Photon.Pun;
using System.Collections;
using UnityEngine;


public class HealingRay : MonoBehaviour
{
    public Transform firePoint;
    public float rayLength = 50f;
    public float minDistance = 1.0f;
    public float healAmount = 20f;
    public LineRenderer lineRenderer; // 광선 시각화
    public Transform handTransform; // 광선이 발사될 시작 지점
    public LayerMask targetLayer;
    PhotonView pv;
    // 광선 효과 지속 시간
    public float lineDisplayDuration = 0.2f;

    private void Awake()
    {
        lineRenderer.enabled = false; // 처음에는 선을 비활성화
        lineRenderer.positionCount = 2;
        pv = GetComponent<PhotonView>();
    }
    public void FireHealingRay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 100f))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100f);

        Vector3 rayStartPoint = new Vector3(firePoint.position.x, firePoint.position.y, firePoint.position.z);
        Vector3 rayDirection = ray.direction;

        Vector3 spawnPos = rayStartPoint + rayDirection * minDistance;


        bool isHit = Physics.Raycast(spawnPos, rayDirection, out hit, rayLength, targetLayer);

        Vector3 rayEndPoint;

        if (isHit)
        {
            // 무언가에 맞았다면, 그 지점을 광선의 끝점으로 설정
            rayEndPoint = hit.point;
            Debug.Log("힐 광선이 " + hit.collider.name + "에 맞았습니다!");

            // 맞은 대상이 LivingEntity를 가지고 있는지 확인
            PlayerHealth hitLivingEntity = hit.collider.GetComponent<PlayerHealth>();
            if (hitLivingEntity != null)
            {
                hitLivingEntity.RestoreHealth(healAmount);
                Debug.Log(hit.collider.name + "에게 " + healAmount + "만큼 치료!");
            }
        }
        else
        {
            // 아무것도 맞지 않았다면, 광선은 최대 길이까지 쭉 뻗어나감
            rayEndPoint = spawnPos + rayDirection * rayLength;
            
        }
        pv.RPC("RPC_ShowHealingRay", RpcTarget.All, spawnPos, rayEndPoint);
        
    }
    [PunRPC]
    void RPC_ShowHealingRay(Vector3 startPoint, Vector3 endPoint)
    {
        StartCoroutine(ShowHealingRay(startPoint, endPoint));
    }
    private IEnumerator ShowHealingRay(Vector3 startPoint, Vector3 endPoint)
    {
        // LineRenderer 활성화
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, startPoint); // 광선 시작점
        lineRenderer.SetPosition(1, endPoint); // 광선 끝점

        // 선 색상 설정 (선택 사항)
        /* lineRenderer.startColor = Color.green;
         lineRenderer.endColor = Color.cyan;*/
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.05f;

        // lineDisplayDuration 동안 대기
        yield return new WaitForSeconds(lineDisplayDuration);

        // LineRenderer 비활성화
        lineRenderer.enabled = false;
    }
}
