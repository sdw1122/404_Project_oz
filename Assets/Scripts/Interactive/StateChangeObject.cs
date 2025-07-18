using UnityEngine;
using Photon.Pun;

public class StateChangeObject : InteractableBase
{
    [Header("상태별 재질")]
    [SerializeField] private Material eraserMaterial;
    [SerializeField] private Material penMaterial;

    private Renderer objectRenderer;
    private Collider objectCollider;

    protected override void Awake()
    {
        base.Awake();
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();
    }

    public override void Interact(PlayerController player)
    {
        // 상호작용이 가능한지 먼저 확인합니다. (가장 중요한 부분)
        if (!CanInteract(player)) return;

        // 기존 로직은 그대로 사용합니다.
        if (player.job == "eraser")
        {
            pv.RPC("ChangeState", RpcTarget.All, true);
        }
        else if (player.job == "pen")
        {
            pv.RPC("ChangeState", RpcTarget.All, false);
        }
    }

    [PunRPC]
    private void ChangeState(bool isErased)
    {
        if (isErased)
        {
            objectRenderer.material = eraserMaterial;
            objectCollider.isTrigger = true;
        }
        else
        {
            objectRenderer.material = penMaterial;
            objectCollider.isTrigger = false;
        }
    }
}