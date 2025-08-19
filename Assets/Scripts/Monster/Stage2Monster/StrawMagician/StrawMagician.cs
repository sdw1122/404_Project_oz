using Photon.Pun;
using System.Collections;
using UnityEngine;



public class StrawMagician : Enemy
{
    public bool IsGroggy() => isGroggy;
    private bool isGroggy = false;
    
    public float speed = 4.0f;
    float distanceToTarget;
    Straw_MagicArrow straw_MagicArrow;
    Straw_FireBall straw_FireBall;
    Straw_BindCircle straw_BindCircle;
    Straw_Teleport straw_Teleport;
    StrawMagician_State _currentState;
    public bool canAct = true;
    public float groggyTime=3.0f;
    [Header("후퇴 설정")]
    public float runDuration = 3.0f; 
    public float runSpeed = 1.5f; 
    public enum StrawMagician_State
    {
        Attack, Fireball, Bind, Idle
    }
    public enum StrawMagician_Mode
    {
        Normal, Run, Vulnerable
    }
    [SerializeField]
    private StrawMagician_Mode currentMode;
    public override void Awake()
    {
    base.Awake();
        straw_MagicArrow=GetComponent<Straw_MagicArrow>();
        straw_FireBall=GetComponent<Straw_FireBall>();
        straw_BindCircle=GetComponent<Straw_BindCircle>();
        TryGetComponent<Straw_Teleport>(out straw_Teleport);
        navMeshAgent.speed = speed;
        attackRange = straw_MagicArrow.range;
        
    }

    private IEnumerator DelayedAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dead) yield break;
        canAct = true;
        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = true;
        
    }
    public void OnDamaged()
    {
        if (straw_Teleport == null) return;
        straw_Teleport.ReduceTeleportCooldown();
    }
    public override void Attack()
    {
        switch (_currentState)
        {
            case StrawMagician_State.Attack:
                pv.RPC("StrawMagician_AttackRPC", RpcTarget.All);
                
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath(); // 목적지 초기화
         
                navMeshAgent.velocity = Vector3.zero;
                StartCoroutine(DelayedAction(2.3f));
                _currentState = StrawMagician_State.Idle;
             
                break;
            case StrawMagician_State.Fireball:
                pv.RPC("StrawMagician_FireBallRPC", RpcTarget.All);
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath(); // 목적지 초기화
                navMeshAgent.velocity = Vector3.zero;
                StartCoroutine(DelayedAction(2.3f));
                _currentState = StrawMagician_State.Idle;
          
                break;
            case StrawMagician_State.Bind:
                pv.RPC("StrawMagician_BindCircleRPC", RpcTarget.All);
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath(); // 목적지 초기화
                navMeshAgent.velocity = Vector3.zero;
                StartCoroutine(DelayedAction(2.7f));
                _currentState = StrawMagician_State.Idle;
               
                break;
            case StrawMagician_State.Idle:
                /*enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);*/
                break;
        }
    }
    public void RunFromBind()
    {
        StopAllCoroutines(); // 현재 진행 중인 모든 코루틴 중단
        canAct = true;     

        if (enemyAnimator != null)
        {
            enemyAnimator.speed = 1f;
            enemyAnimator.Play("Move");
            pv.RPC("RPC_StrawRun", RpcTarget.Others);
            enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
            
            pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
            foreach (var param in enemyAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    enemyAnimator.ResetTrigger(param.name);
                }
            }
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.ResetPath();
        navMeshAgent.velocity = Vector3.zero;
        navMeshAgent.updateRotation = true;
        currentMode = StrawMagician_Mode.Run;
        StartCoroutine(RunModeRoutine());
    }
    public IEnumerator Straw_GroggyRoutine()
    {
        isGroggy = true;
        isBinded = true;
        enemyAnimator.Play("Stumble");
        enemyAnimator.SetTrigger("Stumble");

        yield return new WaitForSeconds(groggyTime);

        enemyAnimator.SetTrigger("Get up");

        yield return new WaitForSeconds(3.5f);

        isGroggy = false;
        isBinded = false;
    }
    [PunRPC]
    public void RPC_StrawGroggy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            isGroggy = true;
            isBinded = true;
            pv.RPC("RPC_PlayStumbleAnim", RpcTarget.All);
            StartCoroutine(GroggyTimer());
        }
    }
    [PunRPC]
    public void RPC_PlayStumbleAnim()
    {
        enemyAnimator.Play("Stumble");
        enemyAnimator.SetTrigger("Stumble");
    }
    private IEnumerator GroggyTimer()
    {
        yield return new WaitForSeconds(groggyTime);
        pv.RPC("RPC_PlayGetUpAnim", RpcTarget.All);
        yield return new WaitForSeconds(3.5f);
        pv.RPC("RPC_EndGroggy", RpcTarget.All);
    }
    [PunRPC]
    public void RPC_PlayGetUpAnim()
    {
        enemyAnimator.SetTrigger("Get up");
    }

    [PunRPC]
    public void RPC_EndGroggy()
    {
        isGroggy = false;
        isBinded = false;
    }
    [PunRPC]
    public void RPC_StrawRun()
    {
        enemyAnimator.Play("Move");
    }
    private IEnumerator RunModeRoutine()
    {
        canAct = false;
        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = true;
        float startTime = Time.time;
        while (Time.time < startTime + runDuration)
        {
            if (targetEntity == null || targetEntity.dead) break;
            Vector3 runDirection = (transform.position - targetEntity.transform.position).normalized;
            runDirection.y = 0;
            if (runDirection == Vector3.zero) runDirection = -transform.forward;
            navMeshAgent.SetDestination(transform.position + runDirection * (navMeshAgent.speed * runSpeed));
            yield return null;
        }
        navMeshAgent.isStopped = true;
        canAct = true;
        _currentState = StrawMagician_State.Idle;
        currentMode = StrawMagician_Mode.Normal;
        enemyAnimator.SetFloat("Move", 0f); // 공격 전 Idle자세
        pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
    }
    public override void Update()
    {
        base.Update();
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (!canAct||dead) return;
        if (isGroggy) return;
        if (straw_Teleport!=null&&straw_Teleport.IsReady())
        {
            pv.RPC("PerformTeleportRPC", RpcTarget.All);
            return;
        }
        if (isBinded && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            return;
        }

        if (targetEntity == null || targetEntity.dead)
        {
            _currentState = StrawMagician_State.Idle; // 타겟 없으면 Idle 상태로
            return; 
        }
        distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
        Vector3 dir = targetEntity.transform.position - transform.position;
        dir.y = 0f;
        dir.Normalize();
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        
        transform.rotation = lookRotation;
        Debug.Log("파이어볼 준비 상태 : "+ straw_FireBall.IsReady());
        if (straw_FireBall.IsReady() && distanceToTarget < straw_FireBall.range)
        {
            _currentState = StrawMagician_State.Fireball;
            canAct = false;
            Attack();
            return;
        }
        else if (!straw_FireBall.IsReady() && straw_MagicArrow.IsReady() && distanceToTarget < straw_MagicArrow.range)
        {
            _currentState = StrawMagician_State.Attack;
            canAct = false;
            Attack();
            return;
        }
        else if (straw_BindCircle.IsReady() && !straw_FireBall.IsReady() && !straw_MagicArrow.IsReady() && distanceToTarget < straw_MagicArrow.range)
        {
            _currentState = StrawMagician_State.Bind;
            canAct = false;
            Attack();
            return;
        }
    }
    public override void Die()
    {
        StopAllCoroutines();

        
        base.Die();
    }
}
