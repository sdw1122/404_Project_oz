using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// 이 레버는 상호작용 시 연결된 MovingObject를 이동시키고,
/// 정해진 시간 후에 자동으로 원래 위치로 되돌립니다.
/// </summary>
public class TimedLeverController : InteractableBase
{
    [Header("연결된 오브젝트")]
    [Tooltip("이 레버를 당겼을 때 움직일 MovingObject들을 여기에 연결하세요.")]
    [SerializeField] private MovingObject[] controlledObjects;

    [Header("작동 설정")]
    [Tooltip("오브젝트가 이동한 후, 원래 위치로 돌아오기까지 걸리는 시간(초)입니다.")]
    [SerializeField] private float returnDelay = 30f;

    // 레버가 활성화된 상태인지 확인 (중복 작동 방지용)
    private bool isActivated = false;

    /// <summary>
    /// 플레이어가 이 오브젝트와 상호작용할 수 있는지 확인합니다.
    /// </summary>
    public override bool CanInteract(PlayerController player)
    {
        // 레버가 비활성화 상태일 때만 상호작용 가능하도록 합니다.
        return !isActivated && base.CanInteract(player);
    }

    /// <summary>
    /// 플레이어가 상호작용했을 때 호출되는 메인 함수입니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        // 상호작용 조건을 다시 한번 확인합니다.
        if (!CanInteract(player)) return;

        // 모든 클라이언트에게 레버 활성화를 요청하는 RPC를 보냅니다.
        pv.RPC("ActivateAndStartTimer_RPC", RpcTarget.All);
    }

    /// <summary>
    /// [모든 클라이언트에서 실행됨]
    /// 레버를 활성화하고, 연결된 오브젝트를 움직이며, 타이머를 시작합니다.
    /// </summary>
    [PunRPC]
    private void ActivateAndStartTimer_RPC()
    {
        // 이미 활성화 상태라면 중복 실행하지 않습니다.
        if (isActivated) return;
        isActivated = true;

        // 연결된 MovingObject들이 없으면 경고 메시지를 출력하고 종료합니다.
        if (controlledObjects == null || controlledObjects.Length == 0)
        {
            Debug.LogWarning($"'{gameObject.name}' 레버에 연결된 MovingObject가 없습니다.", this);
            return;
        }

        // 연결된 모든 오브젝트의 이동을 트리거합니다.
        foreach (MovingObject obj in controlledObjects)
        {
            if (obj != null)
            {
                obj.TriggerMovement();
            }
        }

        // 마스터 클라이언트만 되돌아오는 타이머 코루틴을 실행하여 중복을 방지합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ReturnObjectsAfterDelay());
        }
    }

    /// <summary>
    /// [마스터 클라이언트에서만 실행됨]
    /// 지정된 시간만큼 기다린 후, 오브젝트를 원위치로 되돌리는 RPC를 호출합니다.
    /// </summary>
    private IEnumerator ReturnObjectsAfterDelay()
    {
        // 설정된 시간(returnDelay)만큼 대기합니다.
        yield return new WaitForSeconds(returnDelay);

        // 모든 클라이언트에게 오브젝트를 되돌리라는 RPC를 보냅니다.
        pv.RPC("DeactivateAndReturn_RPC", RpcTarget.All);
    }

    /// <summary>
    /// [모든 클라이언트에서 실행됨]
    /// 오브젝트를 다시 원위치로 움직이고, 레버를 다시 상호작용 가능한 상태로 만듭니다.
    /// </summary>
    [PunRPC]
    private void DeactivateAndReturn_RPC()
    {
        // 연결된 모든 오브젝트의 이동을 다시 트리거하여 원위치로 되돌립니다.
        foreach (MovingObject obj in controlledObjects)
        {
            if (obj != null)
            {
                obj.TriggerMovement();
            }
        }

        // 레버를 다시 상호작용 가능한 상태로 변경합니다.
        isActivated = false;
    }
}