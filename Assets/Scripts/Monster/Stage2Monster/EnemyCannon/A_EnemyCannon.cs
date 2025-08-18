using Photon.Pun;
using System.Collections;
using UnityEngine;

public class A_EnemyCannon : MonoBehaviour
{
    [Header("타겟 및 발사체 설정")]
    public string playerTag = "Player"; // 감지할 플레이어의 태그
    public GameObject cannonballPrefab; // 발사할 대포알 프리팹
    public Transform firePoint; // 대포알이 발사될 위치

    [Header("감지 및 공격 설정")]
    [SerializeField]private float detectionRange; // 플레이어 감지 범위
    public float turnSpeed = 5f; // 대포의 회전 속도
    public float aimDuration = 5.5f; // 조준 시간 (초)
    public float fireDelay = 0.5f; // 조준 고정 후 발사까지의 딜레이 (초)
    public float cannonballSpeed = 20f; // 대포알 속도
    public float repeatCooldown = 2f; // 발사 후 다음 조준까지의 대기 시간
    public float cannonDamage = 20f; // 대포알 데미지
    private Transform player; // 플레이어의 Transform
    private LineRenderer aimLine; // 조준선을 그릴 LineRenderer
    private Coroutine aimAndFireCoroutine; // 실행 중인 코루틴을 제어하기 위한 변수
    public float maxWidth = 0.2f;
    public float minWidth = 0.001f;
    public Color aimColor;
    [Header("연사 간격 설정")]
    public bool isAuto;
    public float fireInterval= 0.5f;
    public int fireAmount = 3;
    Animator animator;
    PhotonView pv;
    public AudioSource fireSource;
    void Start()
    {
        aimLine = GetComponent<LineRenderer>();
        aimLine.positionCount = 2;
        aimLine.enabled = false;
        aimLine.alignment = LineAlignment.View; // 항상 카메라를 향하게
        aimLine.material = new Material(Shader.Find("Unlit/Color"));
        aimLine.material.color = aimColor;
        SphereCollider detectionCollider = GetComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRange;
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag(playerTag) && aimAndFireCoroutine == null)
        {
            player = other.transform;
            aimAndFireCoroutine = StartCoroutine(AimAndFire());
            pv.RPC("StartAimingRPC", RpcTarget.Others);
            if (animator != null)
                animator.SetBool("Aiming", true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && other.transform == player)
        {
            if (aimAndFireCoroutine != null)
            {
                StopCoroutine(aimAndFireCoroutine);
                aimAndFireCoroutine = null;
            }

            player = null;
            aimLine.enabled = false;

            if (animator != null)
                animator.SetBool("Aiming", false);
        }
    }
    [PunRPC]
    void StartAimingRPC()
    {
        if (aimAndFireCoroutine == null)
            aimAndFireCoroutine = StartCoroutine(AimAndFire());
    }
    
    private IEnumerator AimAndFire()
    {
        while (true)
        {
            // --- 1단계: 조준 (Aim Duration) ---
            aimLine.enabled = true;
            float aimTimer = 0f;



            while (aimTimer < aimDuration)
            {
                if (player == null)
                {
                    // 플레이어가 조준 중에 나갔다면, 현재 조준/발사 사이클을 중단합니다.
                    aimLine.enabled = false;
                    aimAndFireCoroutine = null;
                    yield break;
                }
                Vector3 targetPosition = player.position + new Vector3(0, 1.2f, 0);
                Debug.Log("Target Position: " + targetPosition);
                // [수정됨] 플레이어를 향해 상하좌우로 회전합니다.
                Vector3 directionToPlayer = targetPosition - firePoint.transform.position;
                Debug.Log("directionToPlayer: " + directionToPlayer);
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

                // [수정됨] 조준선이 항상 포구의 정면으로 나가도록 설정합니다.
                aimLine.SetPosition(0, firePoint.position);
                aimLine.SetPosition(1, firePoint.position + firePoint.forward * 1000f);
                // 너비 조절
                float t = aimTimer / aimDuration; // 0~1
                float width = Mathf.Lerp(maxWidth, minWidth, t); // 점점 얇아짐
                aimLine.startWidth = width;
                aimLine.endWidth = width;
                aimTimer += Time.deltaTime;
                yield return null;
            }

            // --- 2단계: 조준 고정 ---
            // 조준 루프가 끝나면 대포의 회전이 멈추고, 그 방향이 발사 방향이 됩니다.
            // 조준선은 이미 마지막 프레임의 정면 방향으로 고정되어 있으므로 추가 설정이 필요 없습니다.

            // --- 3단계: 발사 대기 (Fire Delay) ---
            yield return new WaitForSeconds(fireDelay);

            // --- 4단계: 발사 ---
            aimLine.enabled = false;
            if (PhotonNetwork.IsMasterClient)
            {
                int shotsToFire = isAuto ? fireAmount : 1;

                for (int i = 0; i < shotsToFire; i++)
                {
                    if (cannonballPrefab != null)
                    {
                        GameObject cannonball = PhotonNetwork.Instantiate("test/" + cannonballPrefab.name, firePoint.position, Quaternion.identity);
                        EnemyCannonBall ecb = cannonball.GetComponent<EnemyCannonBall>();
                        Rigidbody rb = cannonball.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            PlayFireClip();
                            ecb.Initialize(cannonDamage);
                            rb.AddForce(firePoint.forward * cannonballSpeed, ForceMode.VelocityChange);
                        }
                    }

                    // 연사 간격 대기 (마지막 발사 후엔 기다리지 않음)
                    if (isAuto && i < shotsToFire - 1)
                    {
                        yield return new WaitForSeconds(fireInterval);
                    }
                }


            }
            // --- 5단계: 반복 대기 ---
            yield return new WaitForSeconds(repeatCooldown);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // --- ▼▼▼ 수정된 부분 ▼▼▼ ---
        // transform.lossyScale은 부모 오브젝트의 스케일까지 모두 포함한 최종 스케일 값입니다.
        // x, y, z 스케일 중 가장 큰 값을 기준으로 기즈모의 크기를 조절합니다.
        // 이렇게 하면 비균등 스케일(Non-uniform scale)에도 대응할 수 있습니다.
        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        // 계산된 최종 스케일 값을 반지름(detectionRange)에 곱해줍니다.
        Gizmos.DrawWireSphere(transform.position, detectionRange * maxScale);
        // --- ▲▲▲ 수정된 부분 ▲▲▲ ---
    }
    public void PlayFireClip()
    {
        if (fireSource == null) return;
        AudioClip clip = fireSource.clip;
        fireSource.PlayOneShot(clip);
    }
}
