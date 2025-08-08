using UnityEngine;
using System.Collections;
using Photon.Pun;

public class Skill1 : MonoBehaviour
{
    Animator animator;
    PhotonView pv;
    StrawKingRazor razer;

    Vector3 boxOffset = new Vector3(0f, 60f, 40f);

    public GameObject[] warning;    
    public float waitTime = 0f;
    public float attackTime = 0f;    
    public bool isShake = false;
    public bool endAttack = false;
    private string[] platform = { "Platform 1", "Platform 2", "Platform 3" };

    Platform floorA, floorB, floorC;
    Rigidbody rbA, rbB;
    Collider colA, colB;
    private int countA, countB, countC;
    public bool hasShield = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();      
        razer = GetComponent<StrawKingRazor>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isShake)
        {
            waitTime += Time.deltaTime;

            if (waitTime >= 10f) 
            {
                // 랜덤하게 두 개 인덱스 뽑기 (서로 다르게)
                countA = Random.Range(0, platform.Length);
                do
                {
                    countB = Random.Range(0, platform.Length);
                } while (countB == countA);
                
                for (int i = 0; i < platform.Length; i++)
                {
                    if (i != countA && i != countB)
                    {
                        countC = i;
                        break; // 만약 한 개만 찾으면 break
                    }
                }

                // 각각 Platform 참조 얻기
                floorA = GameObject.Find(platform[countA]).GetComponent<Platform>();
                floorB = GameObject.Find(platform[countB]).GetComponent<Platform>();
                floorC = GameObject.Find(platform[countC]).GetComponent<Platform>();

                // 버튼작동
                Transform child = floorC.transform.Find("testButton");
                if (child != null)
                {
                    child.gameObject.SetActive(true);
                }

                // 경고 ON
                warning[countA].SetActive(true);
                warning[countB].SetActive(true);

                isShake = true;
                StartCoroutine(ShakeSequence(10f, 0.2f));
            }
        }

        if (endAttack)
        {
            colA.isTrigger = false;
            colB.isTrigger = false;
            rbA.isKinematic = true;
            rbB.isKinematic = true;

            floorA.transform.localPosition = new Vector3(floorA.transform.localPosition.x, -10f, floorA.transform.localPosition.z);
            floorB.transform.localPosition = new Vector3(floorB.transform.localPosition.x, -10f, floorB.transform.localPosition.z);

            Transform child = floorC.transform.Find("testButton");
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }

            pv.RPC("RPC_DestroyWall", RpcTarget.All);

            waitTime = 0f;
            endAttack = false;
            isShake = false;
        }
        

        // Shield 레이어 마스크 구하기
        int shieldLayer = LayerMask.NameToLayer("Shield");
        int shieldMask = 1 << shieldLayer;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 70f ;
        Vector3 direction = transform.forward;
        float maxDistance = 100f; // 원하는 거리만큼

        if (Physics.Raycast(origin, direction, out hit, maxDistance, shieldMask))
        {
            // Shield 레이어의 오브젝트가 앞에 감지됨!
            Debug.Log("Shield 감지: " + hit.collider.gameObject.name);
            razer.isBlock = true;
            hasShield = true;
        }
        else
        {
            hasShield = false;
        }

    }

    IEnumerator ShakeSequence(float shakeTime, float magnitude)
    {
        // 각각 흔들기
        floorA.StartCoroutine(floorA.Shake(shakeTime, magnitude));
        floorB.StartCoroutine(floorB.Shake(shakeTime, magnitude));
        yield return new WaitForSeconds(shakeTime);

        // 바닥 쳐다보기
        StartCoroutine(SmoothLookAt(floorC.transform.position));

        // 경고 OFF
        warning[countA].SetActive(false);
        warning[countB].SetActive(false);

        // 바닥 떨어짐
        colA = floorA.GetComponent<Collider>();
        colB = floorB.GetComponent<Collider>();
        rbA = floorA.GetComponent<Rigidbody>();
        rbB = floorB.GetComponent<Rigidbody>();
        if (rbA != null) rbA.isKinematic = false;
        if (rbB != null) rbB.isKinematic = false;
        if (colA != null) colA.isTrigger = true;
        if (colB != null) colB.isTrigger = true;

        // 초기화                
        pv.RPC("RPC_Boos2Charge", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_Boos2Charge()
    {
        animator.SetTrigger("ChargeAttack");
    }

    IEnumerator SmoothLookAt(Vector3 targetPos, float speed = 4f)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        float time = 0f;
        float duration = Quaternion.Angle(startRot, targetRot) / (speed * 60f);

        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRot;
    }

    public void Skill1Attack()
    {
        LayerMask targetMask = LayerMask.GetMask("Player", "Enemy");

        // OverlapBox center/size/rotation: 기즈모와 반드시 똑같이!
        Vector3 boxCenter = transform.position
            + transform.right * boxOffset.x
            + transform.up * boxOffset.y
            + transform.forward * boxOffset.z;
        Vector3 boxSize = new Vector3(80f, 50f, 120f);

        // 주의: OverlapBox의 size 인자는 "반 사이즈"입니다!!!
        Collider[] hits = Physics.OverlapBox(
            boxCenter,
            boxSize * 0.5f,    // 반드시 size * 0.5f
            transform.rotation,
            targetMask
        );

        if (hasShield)
        {
            // 필요하면 Debug.Log("Shield가 있어 공격 무효!");
            return;
        }

        foreach (Collider col in hits)
        {
            LivingEntity entity = col.GetComponent<LivingEntity>();
            if (entity != null && !hasShield)
            {
                // 충돌 위치 계산: 내 위치와 가장 가까운 상대 표면
                Vector3 hitPoint = col.ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;
                entity.OnDamage(10000f, hitPoint, hitNormal);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // OverlapBox와 같은 값
        Vector3 boxCenter = transform.position    + transform.right * boxOffset.x + transform.up * boxOffset.y + transform.forward * boxOffset.z;
        Vector3 boxSize = new Vector3(80f, 50f, 120f);

        // 기즈모 색상 및 회전값 적용
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);

        Gizmos.DrawWireCube(Vector3.zero, boxSize); // (Vector3.zero는 matrix로 중심 이동)
    }
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 70f;
        Vector3 direction = transform.forward;
        float maxDistance = 100f;

        Gizmos.color = Color.cyan;

        RaycastHit hit;
        int shieldMask = 1 << LayerMask.NameToLayer("Shield");

        if (Physics.Raycast(origin, direction, out hit, maxDistance, shieldMask))
        {
            // Ray가 히트한 지점까지만 선을 그림
            Gizmos.DrawLine(origin, hit.point);
            // 히트 지점에 구체 표시(예: 반지름 1)
            Gizmos.DrawSphere(hit.point, 1f);
        }
        else
        {
            // 아무것도 안 맞으면 끝까지 그리기
            Gizmos.DrawRay(origin, direction * maxDistance);
        }
    }

    //뺄수도
    public void PauseAnimation()
    {
        animator.speed = 0f; // 애니메이션 일시 정지
        StartCoroutine(ResumeAfterDelay(9f)); // 10초 뒤 재개(원하는 시간으로 조절)
    }

    IEnumerator ResumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.speed = 1f; // 애니메이션 재생 재개
    }

    public void EndAnimation()
    {
        endAttack = true;
    }

    [PunRPC]
    public void RPC_DestroyWall()
    {
        GameObject wall = GameObject.Find("testWall1(Clone)");
        if (wall != null)
            PhotonNetwork.Destroy(wall);
    }
}
