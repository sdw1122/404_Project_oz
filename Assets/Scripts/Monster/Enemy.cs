using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public abstract class Enemy : LivingEntity
{
    public LayerMask whatIsTarget; // 추적 대상 레이어

    public LivingEntity targetEntity; // 추적 대상
    public NavMeshAgent navMeshAgent; // 경로 계산 AI 에이전트
    public NavMeshObstacle obstacle;

    public ParticleSystem hitEffect; // 피격 시 재생할 파티클 효과
    public AudioSource hitSource;
    public AudioSource stepSource;
    public AudioSource dieSource;
    public AudioSource hurtSource;
    private AudioClip enemyClip;

    public Animator enemyAnimator; // 애니메이터 컴포넌트
    public Renderer enemyRenderer; // 렌더러 컴포넌트
    private Rigidbody rb;        
    public abstract void Attack();

    public float currentHealth;
    public float damage; // 공격력
    public float Atk_Cooldown; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점
    public float attackRange;
    public float DEF_Factor;
    public bool isBinded=false;
    public bool isFirstChase = true;
    private Color originalColor;
    public bool isAttacking = false;
    public bool isHit = false;

    public bool isRotatingToTarget = false;  // 초깃값: false
    public float angleToTarget;

    public PhotonView pv;
    public float chaseTarget;
    int lastAttacker;
    [Header("지혜 수치 설정")]
    public int wisdomAmount;

    [Header("UI 설정")]
    [Tooltip("몬스터 머리 위에 생성될 체력 바")]
    public GameObject healthBarPrefab;

    [Tooltip("체력 바가 생성될 기준 위치")]
    public Transform healthBarPoint;

    private EnemyHealthBarController healthBarController; // 생성된 체력바 컨트롤러를 저장할 변수
    private float lastKnownHealth;
    [SerializeField] private float destroyDelay = 2.0f; // 시체 유지 시간
    public string m_name;

    public bool hasDeathHandler;

    // 추적할 대상이 존재하는지 알려주는 프로퍼티
    public bool hasTarget
    {
        get
        {
            // 추적할 대상이 존재하고, 대상이 사망하지 않았다면 true
            if (targetEntity != null && !targetEntity.dead)
            {
                return true;
            }

            // 그렇지 않다면 false
            return false;
        }
    }

    public virtual void Awake()
    {
        // 초기화
        navMeshAgent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        enemyAnimator = GetComponent<Animator>();
        /*enemyAudioPlayer = GetComponent<AudioSource>();*/
        pv=GetComponent<PhotonView>();
        Debug.Log("Awake: navMeshAgent=" + (navMeshAgent == null ? "NULL" : "OK") + ", pv=" + (pv == null ? "NULL" : "OK"));
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }
        if (gameObject.CompareTag("StrawMagician"))
        {
            originalColor = Color.white;
            
        }
        else { originalColor = enemyRenderer.material.color; }
        Debug.Log("원 색" + originalColor);
        
        rb = GetComponent<Rigidbody>();
        PhotonNetwork.SerializationRate = 20;
        if (enemyRenderer != null)
            enemyRenderer.enabled = true;
        if (enemyAnimator != null)
            enemyAnimator.enabled = true;
        Debug.Log("Enemy Awake - Renderer enabled:" + enemyRenderer.enabled + ", color:" + enemyRenderer.material.color);
        navMeshAgent.enabled = false;

        //체력 바 추가
        if (healthBarPrefab != null && healthBarPoint != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab, healthBarPoint.position, healthBarPoint.rotation, healthBarPoint);
            healthBarController = healthBarObj.GetComponent<EnemyHealthBarController>();

            // Enemy 스크립트를 상속받는 모든 몬스터는
            // 자신의 Inspector 창에서 설정한 m_name 값을 체력 바에 자동으로 표시.
            if (healthBarController != null)
            {
                healthBarController.SetName(m_name);
            }
        }
    }

    // 초기 스펙을 결정하는 셋업 메서드
    public void Setup(EnemyData enemyData)
    {
        
    }

    private void Start()
    {
        lastKnownHealth = health;
        if (!pv.IsMine)
        {
            // 네트워크 위치 동기화 완료 후 워프
            navMeshAgent.Warp(transform.position);
        }
        if (!(this is HangingCitizen))
        {
            navMeshAgent.enabled = true;
        }
        Debug.Log("Enemy Awake pv = " + (pv != null ? pv.ViewID.ToString() : "NULL"));
        // 게임 오브젝트 활성화와 동시에 AI의 추적 루틴 시작
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(UpdatePath());
        }
    }

    public virtual void Update()
    {
        // --- 1. UI 업데이트 로직 (모든 클라이언트에서 실행) ---
        // OnPhotonSerializeView를 통해 자동으로 동기화된 health 값이 이전 프레임과 다른지 확인합니다.
        if (health != lastKnownHealth)
        {
            // 체력 값이 바뀌었다면, 체력 바 UI를 업데이트합니다.
            if (healthBarController != null)
            {
                healthBarController.UpdateHealth(health, startingHealth);
            }
            // 현재 체력 값을 lastKnownHealth에 저장하여 다음 프레임에 비교할 수 있도록 합니다.
            lastKnownHealth = health;
        }

        // --- 2. AI 및 이동 로직 (마스터 클라이언트에서만 실행) ---
        // 마스터 클라이언트가 아니면 AI 로직을 실행하지 않고 여기서 함수를 종료합니다.
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // 이 아래는 기존 Update()에 있던 마스터 클라이언트 전용 로직입니다.
        if (isBinded && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
        }
        currentHealth = health;
        // 추적 대상의 존재 여부에 따라 다른 애니메이션 재생
        /*enemyAnimator.SetBool("HasTarget", hasTarget);*/
        if (targetEntity != null && isRotatingToTarget && !isAttacking)
        {
            Vector3 dir = targetEntity.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                float angleToTarget = Quaternion.Angle(transform.rotation, targetRot);

                float stopThreshold = 5f; // 임계각(도 단위, 2~5 사이에서 조절)

                if (angleToTarget > stopThreshold)
                {
                    // 아직 목표에 충분히 도달하지 않았으면 부드럽게 돌기
                    float slerpSpeed = 10f;
                    float t = Time.deltaTime * slerpSpeed;
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
                }
                else
                {
                    // 각도 차이가 임계값 이하! "딱" 목표 방향으로 고정 후, 회전 멈춤
                    transform.rotation = targetRot;
                    isRotatingToTarget = false; // 여기서 회전 종료
                }

                // angleToTarget 변수는 필요시 계속 갱신
                this.angleToTarget = angleToTarget;
            }
        }
    }


    public virtual bool CanAct()
    {
        return !isAttacking;
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로 갱신
    public virtual IEnumerator UpdatePath()
    {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {
            
            // 추적 로직은 마스터에서만 실행시켜 둘의 Enemy의 움직임을 동기화함.
            if (!PhotonNetwork.IsMasterClient)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }
            if (isBinded)
            {
                if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.updateRotation = false;
                    
                    navMeshAgent.updateRotation = true;
                }
                
            }
            
            if (hasTarget)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);               
                if (dist <= attackRange&&!isBinded)
                {
                    enemyAnimator.SetFloat("Move", 0f); // 공격 전 Idle자세
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
                    if (CanAct() && !isHit)    // (공격 가능한지 자식에게 '질문')
                    {                        
                        Attack();    // -> Attack도 override 해서 자식 전용
                    }
                }
                else if (dist > 20f)
                {
                    if (chaseTarget < 10f)
                    {
                        chaseTarget += 0.25f;
                        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh && !isBinded)
                        {
                            navMeshAgent.isStopped = false;
                            navMeshAgent.SetDestination(targetEntity.transform.position);
                            Debug.Log("목적지설정");
                            if (PhotonNetwork.IsMasterClient)
                            {
                                //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                                pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);
                            }
                            enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                            pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
                        }
                    }
                    else
                    {
                        targetEntity = null;
                        chaseTarget = 0;
                    }
                }
                else if (dist <= 20f)
                {
                    chaseTarget = 0;
                    if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh && !isBinded)
                    {
                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(targetEntity.transform.position);
                        if (PhotonNetwork.IsMasterClient)
                        {
                            //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                            pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);
                        }
                        enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                        pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
                    }
                }
            }
            else
            {
                if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    enemyAnimator.SetFloat("Move", 0f);
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
                }

                Collider[] colliders = Physics.OverlapSphere(transform.position, 20f, whatIsTarget);
                for (int i = 0; i < colliders.Length; i++)
                {
                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();
                    if (livingEntity != null && !livingEntity.dead)
                    {
                        targetEntity = livingEntity;
                        PhotonView targetPV = targetEntity.GetComponent<PhotonView>();

                        if (targetPV != null && pv != null)
                            pv.RPC("SetTarget", RpcTarget.Others, targetPV.ViewID);
                        break;
                    }
                }

            }
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }

    [PunRPC]
    public void SyncLookRotation(Quaternion rot)
    {
        transform.rotation = rot;
        Debug.Log("실행중");
    }

    [PunRPC]
    public void SetTarget(int targetViewId)
    {
        PhotonView targetPV = PhotonView.Find(targetViewId);
        if (targetPV != null)
            targetEntity = targetPV.GetComponent<LivingEntity>();
    }

    [PunRPC]
    public void RPC_ForceSyncPosition(Vector3 pos, Vector3 vel)
    {
        navMeshAgent.Warp(pos); // 즉시 위치 일치
        transform.position = pos;
        rb.linearVelocity = vel;
    }

    [PunRPC]
    public void RPC_BlendIdle(float blendValue)
    {
        enemyAnimator.SetFloat("Move", blendValue);
    }

    [PunRPC]
    public void RPC_BlendRun(float blendValue)
    {
        enemyAnimator.SetFloat("Move", blendValue);
    }

    public void Timer()
    {
        if (chaseTarget < 30f)
        {
            chaseTarget += Time.deltaTime;
        }
    }

    // 사망 처리
    public override void Die()
    {   
        if (dead) return;        
        
        dead = true;
        
        //체력바 숨김
        if (healthBarController != null)
        {
            healthBarController.Hide();
        }

        Debug.Log("dead : " + dead);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;         // 더 이상 중력/물리 영향 X
        }
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;            
        }
        pv.RPC("RPC_EnemyLayer", RpcTarget.All);
        enemyAnimator.SetBool("Die", true);
        pv.RPC("RPC_Die", RpcTarget.Others);
        /*enemyAudioPlayer.PlayOneShot(deathSound);*/
    }

    [PunRPC]
    public void RPC_EnemyLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
    private IEnumerator DestroyAfterDelay()
    {
        // destroyDelay 만큼 기다립니다.
        yield return new WaitForSeconds(destroyDelay);

        // 네트워크 상의 모든 클라이언트에서 이 오브젝트를 파괴합니다.
        // 오브젝트가 이미 파괴되었을 수 있으므로 null 체크를 해주는 것이 안전합니다.
        if (this.gameObject != null)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

    [PunRPC]
    public void RPC_Die()
    {
        enemyAnimator.SetBool("Die", true);
    }
    public void DieState()
    {
        enemyAnimator.SetBool("Die", false);
    }
    public void DieMotion()
    {
        if (enemyAnimator != null)
            enemyAnimator.enabled = false; // 애니메이션 포즈 고정

        Debug.Log("handler : " + hasDeathHandler);

        if (PhotonNetwork.IsMasterClient)
        {
            // 정해진 시간 후에 네트워크 상에서 오브젝트를 파괴합니다.
            PhotonView attacker = PhotonView.Find(lastAttacker);
            if (WisdomManager.Instance != null && attacker != null && attacker.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                WisdomManager.Instance.AddWisdom(wisdomAmount);
            }
            if (!hasDeathHandler||gameObject.CompareTag("StrawMagician"))
                StartCoroutine(DestroyAfterDelay());
        }
        if (!hasDeathHandler)
            StartCoroutine(DestroyAfterDelay());

        base.Die();
    }

    private void UpdateNearestTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 20f, whatIsTarget);

        float closestDistance = float.MaxValue;
        LivingEntity closestEntity = null;

        foreach (Collider col in colliders)
        {
            LivingEntity entity = col.GetComponent<LivingEntity>();
            if (entity != null && !entity.dead)
            {
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }
        }

        if (closestEntity != null)
        {
            targetEntity = closestEntity;
        }
    }   

    private IEnumerator FlashColor()
    {
        Debug.Log("색깔"+originalColor);
       
        enemyRenderer.material.color = Color.red;
        
        yield return new WaitForSeconds(0.15f);

        enemyRenderer.material.color = originalColor;
    }
    [PunRPC]
    private void RPC_FlashColor()
    {
        if (dead) return;
        
        StopCoroutine("FlashColor"); // 중복 방지
        StartCoroutine(FlashColor());
    }
    [PunRPC]
    public void RPC_PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hitEffect != null)
        {
            hitEffect.transform.position = hitPoint;
            hitEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            hitEffect.Play();
            PlayHitClip();
        }

        /*if (enemyAudioPlayer != null && hitSound != null)
            enemyAudioPlayer.PlayOneShot(hitSound);*/

        StopCoroutine("FlashColor");
        StartCoroutine("FlashColor");
    }
    [PunRPC]
    public void RPC_ApplyDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, int attackerViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터만 데미지 처리
        damage *= DEF_Factor;
        lastAttacker = attackerViewID;
        OnDamage(damage, hitPoint, hitNormal);
        GameObject attacker = PhotonView.Find(attackerViewID)?.gameObject;
        if (this is WoodMan woodman)
        {
            woodman.OnDamaged(attacker, damage);
        }else if (this is StrawMagician strawMagician)
        {
            strawMagician.OnDamaged();
        }
    }

    [PunRPC]
    public void RPC_EnemyHit()
    {
        if (dead) return;
        var stateInfo = enemyAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag("Attack") || isAttacking || isHit) return;
        isHit = true;
        enemyAnimator.SetTrigger("Hit");
    }
    public void TriggerOff()
    {
        enemyAnimator.ResetTrigger("Attack");
        enemyAnimator.ResetTrigger("Skill1");
        enemyAnimator.ResetTrigger("Skill2");
    }
    public void EndHit()
    {
        isHit = false;
        isRotatingToTarget = true;
    }
    [PunRPC]
    public void RPC_SetDEF(float value)
    {
        DEF_Factor = value;
    }
    public void PlayHitClip()
    {
        enemyClip = hitSource.clip;
        hitSource.PlayOneShot(enemyClip);
        if (hurtSource != null)
        {
            enemyClip = hurtSource.clip;
            hurtSource.PlayOneShot(enemyClip);
        }
    }
    public void PlayDieClip()
    {
        enemyClip = dieSource.clip;
        dieSource.PlayOneShot(enemyClip);
    }
    public void PlayStepClip()
    {
        enemyClip = stepSource.clip;
        stepSource.PlayOneShot(enemyClip);
    }

    [PunRPC]
    public void HasOnDeath(bool value)
    {
        hasDeathHandler = value; // bool 필드 따로 관리
    }
}
