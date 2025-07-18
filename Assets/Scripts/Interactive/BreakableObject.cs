using UnityEngine;
using System.Collections;
using Photon.Pun;
public class BreakableObject : InteractableBase
{
    private bool isDestroyed = false;

    public override void Interact(PlayerController player)
    {
        if (isDestroyed) return;
        pv.RPC("RequestDestroy", RpcTarget.All);
    }
    [PunRPC]
    private void RequestDestroy()
    {
        // isDestroyed 플래그를 먼저 설정하여 중복 실행을 방지합니다.
        isDestroyed = true;

        // ★★★ 핵심 포인트 ★★★
        // 이 RPC를 수신한 모든 클라이언트 중에서,
        // 오직 이 오브젝트의 소유자(pv.IsMine)만이 파괴 코드를 실행합니다.
        // 이렇게 하면 권한 없는 클라이언트가 파괴를 시도하는 것을 막을 수 있습니다.
        if (pv.IsMine)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }
}
