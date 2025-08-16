using Photon.Pun;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class StrawKing_Poison : MonoBehaviour
{
    [SerializeField]
    private Poison[] poisons; 
    // 스킬 관련 설정 값
    [Tooltip("독 장판 설정")]
    public float poisonInterval = 5f;
    public float damage = 20f;
    public float tik = 1.5f;
    public float duration = 10f;
    [Tooltip("스킬 전체의 쿨타임")]
    public float cooldown = 60f;
    public bool endAttack = true;
    Skill1 skill;
    // 내부 변수
    private float lastAttackTime; // 마지막으로 스킬을 사용한 시간
    private Coroutine skillCoroutine; // 실행 중인 스킬 코루틴을 저장
    Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        skill=GetComponent<Skill1>();
        
        
    }
    public bool IsReady()
    {
        if (!skill.endAttack) return false;
        return Time.time >= lastAttackTime + cooldown;
    }
    [PunRPC]
    public void TyrantRPC()
    {
        lastAttackTime = Time.time;
        endAttack = false;

        // 공격 애니메이션 트리거 
        if (animator != null)
        {
            animator.SetTrigger("Shout");
        }
    }
    public void TryUseSkill()
    {
        // 마스터 클라이언트가 아니거나, 쿨타임이 아직 안됐으면 실행하지 않음
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
        }
        skillCoroutine = StartCoroutine(PoisonCycleRoutine());        
    }
    private IEnumerator PoisonCycleRoutine()
    {
        // poisons 배열에 등록된 모든 장판을 순서대로 활성화
        for (int i = 0; i < poisons.Length; i++)
        {
            Poison currentPoison = poisons[i];
            if (currentPoison != null)
            {
                // Poison 스크립트의 RPC_Activate 함수를 호출하여 모든 클라이언트에서 활성화
                PhotonView pv = currentPoison.GetComponent<PhotonView>();
                if (pv != null)
                {

                    /*currentPoison.gameObject.SetActive(true);*/
                    pv.RPC("RPC_Activate", RpcTarget.All, damage, tik,duration);
                }
            }

            // 다음 장판을 깔기 전까지 5초 대기
            yield return new WaitForSeconds(poisonInterval);
        }
        endAttack = true;
        lastAttackTime = Time.time;
    }
}
