using UnityEngine;
using Photon.Pun;
using System.Collections;

// 상호작용 가능한 레버를 제어하는 스크립트
public class LeverController : InteractableBase
{
    [Header("연결 설정")]
    [Tooltip("이 레버를 당겼을 때 작동시킬 냉각수 분사기를 연결하세요.")]
    [SerializeField] private CoolantSprayer coolantSprayer;

    [Header("작동 설정")]
    [Tooltip("레버를 한 번 사용한 후 다시 사용 가능해질 때까지의 대기 시간(초)입니다.")]
    [SerializeField] private float cooldownTime = 60f;

    private bool isReady = true; // 레버 사용 가능 상태 여부

    public override void Interact(PlayerController player)
    {
        // 레버가 준비 상태이고, 연결된 분사기가 있을 때만 작동
        if (isReady && coolantSprayer != null)
        {
            // 모든 클라이언트에게 레버를 작동시키라는 RPC를 보냄
            pv.RPC("ActivateLever", RpcTarget.All);
        }
    }

    [PunRPC]
    private void ActivateLever()
    {
        isReady = false; // 레버를 비활성화 상태로 변경

        // 연결된 분사기 작동
        if (coolantSprayer != null)
        {
            coolantSprayer.StartSpray();
        }

        // 마스터 클라이언트만 재사용 대기 코루틴을 실행하여 중복 실행 방지
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CooldownRoutine());
        }
    }

    // 일정 시간 후 레버를 다시 사용 가능하게 만드는 코루틴
    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);

        // 모든 클라이언트에게 레버를 다시 활성화하라는 RPC를 보냄
        pv.RPC("ResetLever", RpcTarget.All);
    }

    [PunRPC]
    private void ResetLever()
    {
        isReady = true;
    }

    // 플레이어가 레버와 상호작용 할 수 있는지 확인하는 로직 (기본 로직 수정)
    public override bool CanInteract(PlayerController player)
    {
        // 준비 상태일 때만 상호작용 가능하도록 추가
        return isReady && base.CanInteract(player);
    }
}