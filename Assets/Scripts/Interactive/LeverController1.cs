using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 상호작용 시 연결된 MovingObject들을 작동시키는 레버입니다.
/// </summary>
public class LeverController1 : InteractableBase
{
    [Header("연결된 오브젝트")]
    [Tooltip("이 레버를 당겼을 때 움직일 MovingObject들을 여기에 연결하세요.")]
    [SerializeField] private MovingObject[] controlledObjects;

    // 상호작용한 플레이어들의 ID를 저장하는 리스트 (네트워크 동기화)
    private List<int> interactingPlayerIDs = new List<int>();

    /// <summary>
    /// 플레이어가 레버와 상호작용할 때 호출됩니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        // 로컬 플레이어의 상호작용을 모든 클라이언트에게 알립니다.
        // 플레이어의 고유 ID(ActorNumber)를 RPC로 전달합니다.
        pv.RPC("RegisterInteraction", RpcTarget.AllBuffered, player.GetComponent<PhotonView>().Owner.ActorNumber);
    }

    [PunRPC]
    private void RegisterInteraction(int playerActorNumber)
    {
        // 이미 상호작용한 플레이어인지 확인하여 중복 등록을 방지합니다.
        if (!interactingPlayerIDs.Contains(playerActorNumber))
        {
            interactingPlayerIDs.Add(playerActorNumber);
        }

        // 두 명의 다른 플레이어가 상호작용했는지 확인합니다.
        if (interactingPlayerIDs.Count >= 2)
        {
            // 연결된 오브젝트가 없으면 경고 메시지를 띄우고 종료합니다.
            if (controlledObjects == null || controlledObjects.Length == 0)
            {
                Debug.LogWarning($"'{gameObject.name}' 레버에 연결된 MovingObject가 없습니다.", this);
                return;
            }

            // 연결된 모든 MovingObject에게 움직이라고 명령합니다.
            foreach (MovingObject obj in controlledObjects)
            {
                if (obj != null)
                {
                    // MovingObject에 있는 TriggerMovement() 함수를 호출합니다.
                    obj.TriggerMovement();
                }
            }

            // 작동 후, 상호작용한 플레이어 목록을 초기화하여 다시 사용할 수 있게 합니다. 일단 사용 안함.
            // interactingPlayerIDs.Clear();
        }
    }
}