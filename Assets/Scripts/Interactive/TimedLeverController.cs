using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// 이 레버는 상호작용 시 MovingObject와 FallingLava를 제어하고,
/// 정해진 시간 후에 자동으로 원래 상태로 되돌립니다.
/// </summary>
public class TimedLeverController : InteractableBase
{
    [Header("연결된 오브젝트")]
    [Tooltip("이 레버를 당겼을 때 움직일 MovingObject들을 여기에 연결하세요.")]
    [SerializeField] private MovingObject[] controlledObjects;

    // ----- [수정됨] 하강 용암 제어 -----
    [Tooltip("이 레버로 하강시킬 FallingLava 오브젝트들을 여기에 연결하세요.")]
    [SerializeField] private FallingLava[] fallingLavas;

    [Header("작동 설정")]
    [Tooltip("레버 작동 후 모든 것이 원상 복구되기까지 걸리는 시간(초)입니다.")]
    [SerializeField] private float returnDelay = 30f;

    [Tooltip("용암이 추가 하강을 시작할 시간 (returnDelay 직전 시간)")]
    [SerializeField] private float deeperFallTime = 2.0f;

    private bool isActivated = false;

    public override bool CanInteract(PlayerController player)
    {
        return !isActivated && base.CanInteract(player);
    }

    public override void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        pv.RPC("ActivateAndStartTimer_RPC", RpcTarget.All);
    }

    [PunRPC]
    private void ActivateAndStartTimer_RPC()
    {
        if (isActivated) return;
        isActivated = true;

        // 연결된 MovingObject들을 움직입니다.
        if (controlledObjects != null && controlledObjects.Length > 0)
        {
            foreach (MovingObject obj in controlledObjects)
            {
                if (obj != null) obj.TriggerMovement();
            }
        }

        // 연결된 하강 용암들을 활성화하고 1단계 하강시킵니다.
        if (fallingLavas != null && fallingLavas.Length > 0)
        {
            foreach (FallingLava lava in fallingLavas)
            {
                if (lava != null) lava.ActivateAndFall();
            }
        }

        // 마스터 클라이언트만 복구 타이머를 시작합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ReturnAfterDelay());
        }
    }

    // ----- [수정됨] 복구 코루틴 로직 변경 -----
    private IEnumerator ReturnAfterDelay()
    {
        // returnDelay 시간보다 deeperFallTime 만큼 먼저 대기합니다.
        float firstWaitTime = returnDelay - deeperFallTime;
        if (firstWaitTime > 0)
        {
            yield return new WaitForSeconds(firstWaitTime);
        }

        // 용암을 2단계로 추가 하강시킵니다.
        pv.RPC("TriggerDeeperFall_RPC", RpcTarget.All);

        // 남은 deeperFallTime 만큼 더 대기합니다.
        yield return new WaitForSeconds(deeperFallTime);

        // 모든 것을 원상 복구합니다.
        pv.RPC("DeactivateAndReturn_RPC", RpcTarget.All);
    }

    [PunRPC]
    private void TriggerDeeperFall_RPC()
    {
        if (fallingLavas != null && fallingLavas.Length > 0)
        {
            foreach (FallingLava lava in fallingLavas)
            {
                if (lava != null) lava.FallDeeper();
            }
        }
    }

    [PunRPC]
    private void DeactivateAndReturn_RPC()
    {
        // 움직였던 MovingObject들을 원래 위치로 되돌립니다.
        if (controlledObjects != null)
        {
            foreach (MovingObject obj in controlledObjects)
            {
                if (obj != null) obj.TriggerMovement();
            }
        }

        // 하강했던 용암들을 원래 위치로 복귀시키고 비활성화합니다.
        if (fallingLavas != null && fallingLavas.Length > 0)
        {
            foreach (FallingLava lava in fallingLavas)
            {
                if (lava != null) lava.ResetAndDeactivate();
            }
        }

        isActivated = false;
    }
}