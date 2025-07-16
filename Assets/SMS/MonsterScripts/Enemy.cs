using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : LivingEntity
{
    public LayerMask whatIsTarget; // 추적 대상 레이어

    private LivingEntity targetEntity; // 추적 대상
    private NavMeshAgent navMeshAgent; // 경로 계산 AI 에이전트

    public ParticleSystem hitEffect; // 피격 시 재생할 파티클 효과
    /*public AudioClip deathSound; // 사망 시 재생할 소리
    public AudioClip hitSound; // 피격 시 재생할 소리*/

    private Animator enemyAnimator; // 애니메이터 컴포넌트
    /*private AudioSource enemyAudioPlayer; // 오디오 소스 컴포넌트*/
    private Renderer enemyRenderer; // 렌더러 컴포넌트

    public float currentHealth;
    public float damage = 20f; // 공격력
    public float timeBetAttack = 0.5f; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점
    
    public bool isBinded=false;
    private Color originalColor;

    PhotonView pv;
    // 추적할 대상이 존재하는지 알려주는 프로퍼티
    private bool hasTarget
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
        enemyAnimator = GetComponent<Animator>();
        /*enemyAudioPlayer = GetComponent<AudioSource>();*/
        pv=GetComponent<PhotonView>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        originalColor = enemyRenderer.material.color;
    }

    // 좀비 AI의 초기 스펙을 결정하는 셋업 메서드
    public void Setup(EnemyData enemyData)
    {
        startingHealth = enemyData.Max_HP;
        health = enemyData.Max_HP;
        damage = enemyData.Atk_Damage;
        navMeshAgent.speed = enemyData.speed;
        /*originalColor = enemyData.skinColor;
        enemyRenderer.material.color = enemyData.skinColor;*/
    }

    private void Start()
    {
        // 게임 오브젝트 활성화와 동시에 AI의 추적 루틴 시작
        StartCoroutine(UpdatePath());
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isBinded) navMeshAgent.isStopped = true;
        // 추적 대상의 존재 여부에 따라 다른 애니메이션 재생
        /*enemyAnimator.SetBool("HasTarget", hasTarget);*/
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로 갱신
    private IEnumerator UpdatePath()
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
            if (hasTarget)
            {
                if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = false;
                }
                
                navMeshAgent.SetDestination(targetEntity.transform.position);

            }
            else
            {
                if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                }

                Collider[] colliders = Physics.OverlapSphere(transform.position, 20f, whatIsTarget);
                for (int i = 0; i < colliders.Length; i++)
                {
                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();
                    if (livingEntity != null && !livingEntity.dead)
                    {
                        targetEntity = livingEntity;
                        break;
                    }
                }

            }
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }

    // 데미지를 입었을 때 실행할 처리
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (dead) return;

        // 마스터만 데미지 처리
        if (!PhotonNetwork.IsMasterClient) return;
        // 모든 클라에게 연출 동기화
        /*pv.RPC("RPC_PlayHitEffect", RpcTarget.AllViaServer, hitPoint, hitNormal);*/
        // 데미지 적용
        base.OnDamage(damage, hitPoint, hitNormal);

        
    }

    // 사망 처리
    public override void Die()
    {
        // LivingEntity의 Die()를 실행하여 기본 사망 처리 실행
        base.Die();

        Collider[] enemyColliders = GetComponents<Collider>();
        for (int i = 0; i < enemyColliders.Length; i++)
        {
            enemyColliders[i].enabled = false;
        }
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
        }
        navMeshAgent.enabled = false;
        enemyAnimator.SetTrigger("Die");
        /*enemyAudioPlayer.PlayOneShot(deathSound);*/
    }

    private void OnTriggerStay(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient || dead) return;

        if (Time.time >= lastAttackTime + timeBetAttack)
        {
            
            LivingEntity attackTarget = other.GetComponent<LivingEntity>();
            
            if (attackTarget != null && !attackTarget.dead)
            {
                
                lastAttackTime = Time.time;

                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;

                attackTarget.OnDamage(damage, hitPoint, hitNormal);
                UpdateNearestTarget();
            }
        }
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
    void RPC_PlayHitEffect(Vector3 hitPoint, Vector3 hitNormal)
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
    public void RPC_ApplyDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터만 데미지 처리
        OnDamage(damage, hitPoint, hitNormal);
    }
}
