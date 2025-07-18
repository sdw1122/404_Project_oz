using UnityEngine;
using Photon.Pun;

public abstract class InteractableBase : MonoBehaviour
{
    protected PhotonView pv;

    [Header("상호작용 규칙")]
    [Tooltip("이 오브젝트와 상호작용할 수 있는 직업을 설정합니다.")]
    public EInteractionType interactionType = EInteractionType.Both; // 기본값은 '모두 가능'
    [Tooltip("이 오브젝트의 개별 상호작용 거리입니다. 0으로 설정하면 플레이어의 기본 거리를 따릅니다.")]
    [SerializeField] private float customInteractRange = 0f;

    protected virtual void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogError($"'{gameObject.name}' 오브젝트에 PhotonView 컴포넌트가 없습니다.", this);
        }
    }

    public float GetEffectiveRange(float defaultRange)
    {
        // customInteractRange가 0보다 크면 그 값을 사용하고, 아니면 defaultRange를 사용
        return customInteractRange > 0f ? customInteractRange : defaultRange;
    }

    /// <summary>
    /// 플레이어가 이 오브젝트와 상호작용할 수 있는지 확인합니다.
    /// </summary>
    public virtual bool CanInteract(PlayerController player)
    {
        if (player == null) return false;

        switch (interactionType)
        {
            case EInteractionType.PenOnly:
                return player.job == "pen";
            case EInteractionType.EraserOnly:
                return player.job == "eraser";
            case EInteractionType.Both:
                return player.job == "pen" || player.job == "eraser";
            default:
                return false;
        }
    }

    // 실제 상호작용 로직 (이 부분은 변경 없음)
    public abstract void Interact(PlayerController player);
}