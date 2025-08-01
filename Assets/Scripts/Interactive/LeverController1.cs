using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class LeverController1 : InteractableBase
{
    [Header("연결된 오브젝트")]
    [Tooltip("이 레버를 당겼을 때 움직일 MovingObject들을 여기에 연결하세요.")]
    [SerializeField] private MovingObject[] controlledObjects;

    [SerializeField] private Tower3MonsterSpawner monsterSpawner;

    private List<int> interactingPlayerIDs = new List<int>();

    // ▼▼▼ [추가] 레버가 이미 활성화되었는지 확인하는 변수 ▼▼▼
    private bool hasBeenActivated = false;

    private const int IGNORE_INTERACTION_LAYER = 2;

    public override void Interact(PlayerController player)
    {
        // 이미 활성화된 레버라면 아무것도 하지 않음
        if (hasBeenActivated) return;

        pv.RPC("RegisterInteraction", RpcTarget.AllBuffered, player.GetComponent<PhotonView>().Owner.ActorNumber);
    }
        
    [PunRPC]
    private void RegisterInteraction(int playerActorNumber)
    {
        if (hasBeenActivated) return;

        /*if (!interactingPlayerIDs.Contains(playerActorNumber))
        {
            interactingPlayerIDs.Add(playerActorNumber);
        }*/
        interactingPlayerIDs.Add(playerActorNumber);

        if (interactingPlayerIDs.Count >= 2)
        {
            hasBeenActivated = true;
            gameObject.layer = IGNORE_INTERACTION_LAYER;

            // ----- [수정됨] 마스터 클라이언트만 아래 로직을 실행하도록 변경 -----
            if (PhotonNetwork.IsMasterClient)
            {
                // 1. MovingObject 작동 로직
                if (controlledObjects != null && controlledObjects.Length > 0)
                {
                    foreach (MovingObject obj in controlledObjects)
                    {
                        if (obj != null)
                        {
                            obj.TriggerMovement();
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"'{gameObject.name}' 레버에 연결된 MovingObject가 없습니다.", this);
                }

                // 2. 몬스터 스포너 작동 로직
                if (monsterSpawner != null)
                {
                    monsterSpawner.ActivateSpawner();
                }
                else
                {
                    Debug.LogWarning($"'{gameObject.name}' 레버에 연결된 몬스터 스포너가 없습니다.", this);
                }
            }
            // ----- [수정됨] -----
        }
    }
}