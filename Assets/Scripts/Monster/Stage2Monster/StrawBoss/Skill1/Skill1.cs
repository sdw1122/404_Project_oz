using UnityEngine;
using System.Collections;
using Photon.Pun;

public class Skill1 : MonoBehaviour
{
    Animator animator;
    PhotonView pv;
    public StrawKingRazor razer;
    public WisdomCannon[] cannons;

    Vector3 boxOffset = new Vector3(0f, 60f, 40f);

    public GameObject[] warning;    
    public float waitTime = 0f;
    public float attackTime = 0f;    
    public bool isShake = false;
    public bool endAttack = true;
    public bool isReady = false;
    private string[] platform = { "Platform 1", "Platform 2", "Platform 3" };
    public float cooldown = 30f; // 스킬 쿨타임
    private float lastSkillTime; // 마지막 사용 시간
    public StrawKing_Poison poison;

    Platform floorA, floorB, floorC;
    Rigidbody rbA, rbB;
    Collider colA, colB;
    private int countA, countB, countC;
    public bool hasShield = false;
    bool isHit=false;
    private void Start()
    {
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();      
        razer = GetComponent<StrawKingRazor>();
        poison=GetComponent<StrawKing_Poison>();
    }
    public void SetHit()
    {
        isHit = true;
    }
    public bool IsReady()
    {
        if (!poison.endAttack) return false;
        return Time.time >= lastSkillTime + cooldown;
    }

    [PunRPC]
    public void StartSkill()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        endAttack = false;
        lastSkillTime = Time.time;
        foreach (WisdomCannon cannon in cannons)
        {
            cannon.isSkill1 = true; // 대포 스크립트에서 상호작용 검사시 이 값 체크
        }
        StartCoroutine(AbsorbSequence());
    }
    private IEnumerator AbsorbSequence()
    {
        // 마스터 : 랜덤 플랫폼 인덱스 결정
        int indexA = Random.Range(0, platform.Length);
        int indexB;
        do
        {
            indexB = Random.Range(0, platform.Length);
        } while (indexB == indexA);
        int indexC = 3 - indexA - indexB; // 나머지 안전지대 인덱스

        // 마스터-> 모두 : 플랫폼 경고 표시
        pv.RPC(nameof(RPC_ShowWarnings), RpcTarget.All, indexA, indexB, indexC);

        

        // 마스터-> 모두 : 플랫폼 흔들림 및 낙하
        pv.RPC(nameof(RPC_DropPlatforms), RpcTarget.All, indexA, indexB,indexC);

        // 경고 표시,10초간 흔들림
        yield return new WaitForSeconds(10f);

        // 마스터-> 모두 : 경고 해제
        pv.RPC(nameof(RPC_HideWarnings), RpcTarget.All, indexA, indexB);        
        // 모두 : 차징 애니매이션
        pv.RPC(nameof(RPC_Boss2Charge), RpcTarget.All);
        yield return new WaitUntil(() => endAttack == true);
        // 발판 복구
        if (endAttack) 
        {
            pv.RPC(nameof(RPC_RestorePlatForms), RpcTarget.All, indexA, indexB, indexC);
            pv.RPC("RPC_DestroyWall", RpcTarget.MasterClient);            
        }
    }
    [PunRPC]
    private void RPC_ShowWarnings(int unsafeIndexA, int unsafeIndexB, int safeIndexC)
    {
        // 마스터가 정해준 '동일한' 인덱스로 모든 클라이언트가 시각 효과를 처리
        warning[unsafeIndexA].SetActive(true);
        warning[unsafeIndexB].SetActive(true);

        Platform floorC = GameObject.Find(platform[safeIndexC]).GetComponent<Platform>();
        Transform button = floorC.transform.Find("testButton");
        if (button != null)
        {
            button.gameObject.SetActive(true);
        }
    }
    [PunRPC]
    private void RPC_DropPlatforms(int indexA, int indexB,int indexC)
    {
        Platform floorA = GameObject.Find(platform[indexA]).GetComponent<Platform>();
        Platform floorB = GameObject.Find(platform[indexB]).GetComponent<Platform>();
        Platform floorC = GameObject.Find(platform[indexC]).GetComponent<Platform>();
        // 흔들림 및 낙하
        StartCoroutine(DropSequence(floorA));
        StartCoroutine(DropSequence(floorB));
        StartCoroutine(SmoothLookAt(floorC.transform.position));
        /*// 경고 이펙트 끄기
        warning[indexA].SetActive(false);
        warning[indexB].SetActive(false);*/
    }
    [PunRPC]
    private void RPC_HideWarnings(int indexA, int indexB)
    {
        warning[indexA].SetActive(false);
        warning[indexB].SetActive(false);
    }
    private IEnumerator DropSequence(Platform floor)
    {
        // 10초간 흔들기
        yield return StartCoroutine(floor.Shake(10f, 0.2f));

        //떨어뜨리기
        Collider col = floor.GetComponent<Collider>();
        Rigidbody rb = floor.GetComponent<Rigidbody>();
        if (col != null) col.isTrigger = true;
        if (rb != null) rb.isKinematic = false;
    }
    [PunRPC]
    private void RPC_RestorePlatForms(int indexA,int indexB,int indexC)
    {
        Platform floorA = GameObject.Find(platform[indexA]).GetComponent<Platform>();
        Platform floorB = GameObject.Find(platform[indexB]).GetComponent<Platform>();
        Platform floorC = GameObject.Find(platform[indexC]).GetComponent<Platform>();
        colA = floorA.GetComponent<Collider>();
        colB = floorB.GetComponent<Collider>();
        rbA = floorA.GetComponent<Rigidbody>();
        rbB = floorB.GetComponent<Rigidbody>();
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

        lastSkillTime = Time.time;

    }
    

    [PunRPC]
    public void RPC_Boss2Charge()
    {
        animator.SetTrigger("ChargeAttack");
    }

    IEnumerator SmoothLookAt(Vector3 targetPos, float speed = 4f)
    {
        Debug.Log("시선 호출");
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
        // Shield 레이어 마스크 구하기-------------------추가
        int shieldLayer = LayerMask.NameToLayer("Shield");
        int shieldMask = 1 << shieldLayer;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 70f;
        Vector3 direction = transform.forward;
        float maxDistance = 100f; // 원하는 거리만큼

        if (Physics.Raycast(origin, direction, out hit, maxDistance, shieldMask))
        {
            // Shield 레이어의 오브젝트가 앞에 감지됨!
            Debug.Log("Shield 감지: " + hit.collider.gameObject.name);
            Debug.Log("razer: " + (razer == null ? "null" : razer.ToString()));
            razer.isBlock = true;
            hasShield = true;
        }
        else
        {
            hasShield = false;
        }
        if (hasShield)
        {
            // 필요하면 Debug.Log("Shield가 있어 공격 무효!");
            return;
        }

        foreach (Collider col in hits)
        {
            LivingEntity entity = col.GetComponent<LivingEntity>();
            if (entity != null && !hasShield&&this.GetComponent<LivingEntity>()!=entity)
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
        foreach (WisdomCannon cannon in cannons)
        {
            cannon.isSkill1 = false; // 대포 스크립트에서 상호작용 검사시 이 값 체크
        }
    }

    [PunRPC]
    public IEnumerator RPC_DestroyWall()
    {
        GameObject wall = GameObject.Find("Shield_03(Clone)");
        if (wall != null)
        {
            PhotonView wallView = wall.GetComponent<PhotonView>();
            if (wallView != null)
            {
                // 소유권 이전
                wallView.RequestOwnership();

               // 이전 대기
                yield return new WaitForSeconds(0.5f);

               
                PhotonNetwork.Destroy(wall);
            }
        }
    }
}
