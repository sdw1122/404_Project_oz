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
    /*public AudioClip deathSound; // 사망 시 재생할 소리
    public AudioClip hitSound; // 피격 시 재생할 소리*/

    public Animator enemyAnimator; // 애니메이터 컴포넌트
    /*private AudioSource enemyAudioPlayer; // 오디오 소스 컴포넌트*/
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
    

    public PhotonView pv;
    public float chaseTarget;

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

    private void Awake()
    {
        // 초기화
        navMeshAgent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        enemyAnimator = GetComponent<Animator>();
        /*enemyAudioPlayer = GetComponent<AudioSource>();*/
        pv=GetComponent<PhotonView>();
        Debug.Log("Awake: navMeshAgent=" + (navMeshAgent == null ? "NULL" : "OK") + ", pv=" + (pv == null ? "NULL" : "OK"));
        enemyRenderer = GetComponentInChildren<Renderer>();
        
        originalColor = enemyRenderer.material.color;
        rb = GetComponent<Rigidbody>();
        PhotonNetwork.SerializationRate = 20;
        if (enemyRenderer != null)
            enemyRenderer.enabled = true;
        if (enemyAnimator != null)
            enemyAnimator.enabled = true;
        Debug.Log("Enemy Awake - Renderer enabled:" + enemyRenderer.enabled + ", color:" + enemyRenderer.material.color);
        navMeshAgent.enabled = false;
    }

    // 초기 스펙을 결정하는 셋업 메서드
    public void Setup(EnemyData enemyData)
    {
        if (startingHealth <= 0f) // 인스펙터에서 0이면만 덮어씀
            startingHealth = enemyData.Max_HP;
        if (health <= 0f)
            health = enemyData.Max_HP;
        if (damage <= 0f)
            damage = enemyData.Atk_Damage;
        if (navMeshAgent.speed <= 0f)
            navMeshAgent.speed = enemyData.speed;
        DEF_Factor = enemyData.DEF_Factor;
        /*originalColor = enemyData.skinColor;
        enemyRenderer.material.color = enemyData.skinColor;*/
    }

    private void Start()
    {
        if (!pv.IsMine)
        {
            // 네트워크 위치 동기화 완료 후 워프
            navMeshAgent.Warp(transform.position);
        }
        navMeshAgent.enabled = true; // 위치 맞춘 뒤 에이전트 켜기
        Debug.Log("Enemy Awake pv = " + (pv != null ? pv.ViewID.ToString() : "NULL"));
        // 게임 오브젝트 활성화와 동시에 AI의 추적 루틴 시작
        StartCoroutine(UpdatePath());        
    }

    public virtual void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isBinded && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            /*rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            enemyAnimator.SetFloat("Blend", 1f); // 공격 전 Idle자세
            pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);*/
        }
        currentHealth = health;
        // 추적 대상의 존재 여부에 따라 다른 애니메이션 재생
        /*enemyAnimator.SetBool("HasTarget", hasTarget);*/
    }


    public virtual bool CanAct()
    {
        return true;
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로 갱신
    public virtual IEnumerator UpdatePath()
    {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {
            if (isBinded)
            {
                if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.updateRotation = false;
                    // 바인드 중엔 달리기 애니매이션 출력
                    enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                    pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
                    navMeshAgent.updateRotation = true;
                }
                
            }
            // 추적 로직은 마스터에서만 실행시켜 둘의 Enemy의 움직임을 동기화함.
            if (!PhotonNetwork.IsMasterClient)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }
            if (hasTarget)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);               
                if (dist <= attackRange&&!isBinded)
                {
                    enemyAnimator.SetFloat("Move", 0f); // 공격 전 Idle자세
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
                    if (CanAct())    // (공격 가능한지 자식에게 '질문')
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
        enemyAnimator.SetTrigger("Die");
        pv.RPC("RPC_Die", RpcTarget.Others);
        /*enemyAudioPlayer.PlayOneShot(deathSound);*/
    }

    [PunRPC]
    public void RPC_Die()
    {
        enemyAnimator.SetTrigger("Die");
    }
    public void DieMotion()
    {
        if (enemyAnimator != null)
            enemyAnimator.enabled = false; // 애니메이션 포즈 고정

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
        OnDamage(damage, hitPoint, hitNormal);
        GameObject attacker = PhotonView.Find(attackerViewID)?.gameObject;
        if (this is WoodMan woodman)
        {
            woodman.OnDamaged(attacker, damage);
        }
    }

    [PunRPC]
    public void RPC_EnemyHit()
    {
        enemyAnimator.SetTrigger("Hit");
    }
}
