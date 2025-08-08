using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class InteractiveController : MonoBehaviour
{
    public ParticleSystem interactEffect;
    public AudioSource interactSource;
    public AudioClip interactClip;
    [Header("플레이어 컴포넌트")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;

    [Header("상호작용 설정")]
    [Tooltip("기본 상호작용 거리. 개별 설정이 없는 오브젝트에 이 거리가 적용됩니다.")]
    [SerializeField] private float defaultInteractRange = 3f;
    [SerializeField] private LayerMask interactLayer;

    [Tooltip("상호작용을 감지할 최대 거리입니다. 개별 오브젝트의 상호작용 거리보다 길어야 합니다.")]
    [SerializeField] private float maxDetectionDistance = 150f;

    // [SerializeField] private GameObject interactUI; // 이 변수명을 바꿔서 역할을 명확히 합니다.
    [Header("UI 설정")]
    [Tooltip("플레이어 프리팹의 자식으로 있는 상호작용 UI 오브젝트를 직접 연결하세요.")]
    [SerializeField] private GameObject interactUIObject; // 프리팹이 아닌, 씬에 있는 실제 오브젝트를 연결할 변수

    private PhotonView pv;
    private InteractableBase currentInteractable;

    Animator animator;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();

        // Instantiate 코드를 삭제하고, 대신 연결된 UI가 있는지 확인하고 비활성화합니다.
        if (interactUIObject != null)
        {
            interactUIObject.SetActive(false);
        }
        else
        {
            Debug.LogError("상호작용 UI 오브젝트(interactUIObject)가 할당되지 않았습니다!", this);
        }
    }

    void Update()
    {
        if (!pv.IsMine) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // 레이캐스트의 최대 거리를 defaultInteractRange 대신 maxDetectionDistance로 변경합니다.
        if (Physics.Raycast(ray, out RaycastHit hit, maxDetectionDistance, interactLayer))
        {            
            InteractableBase interactable = hit.collider.GetComponent<InteractableBase>();

            if (interactable != null)
            {
                // 오브젝트의 유효 상호작용 거리를 가져옵니다.
                float effectiveRange = interactable.GetEffectiveRange(defaultInteractRange);

                // 플레이어와 오브젝트 사이의 실제 거리가 유효 거리 이내이고,
                // 상호작용이 가능한 상태인지 함께 체크합니다.                
                if (hit.distance <= effectiveRange && interactable.CanInteract(playerController))
                {                    
                    currentInteractable = interactable;
                    if (interactUIObject != null) interactUIObject.SetActive(true);
                }
                else
                {
                    // 거리가 멀거나 조건이 맞지 않으면 상호작용을 초기화합니다.
                    ClearInteraction();
                }
            }
            else
            {
                ClearInteraction();
            }
        }
        else
        {
            ClearInteraction();
        }
    }

    private void ClearInteraction()
    {
        
        currentInteractable = null;
        // if (uiObjectInstance != null) uiObjectInstance.SetActive(false);
        if (interactUIObject != null) interactUIObject.SetActive(false); // 직접 연결된 오브젝트를 비활성화
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && currentInteractable != null && pv.IsMine)
        {
            currentInteractable.Interact(playerController);
            interactEffect.Play();
            interactSource.PlayOneShot(interactClip);
            animator.SetTrigger("Interactive");
            pv.RPC("RPC_Interactive", RpcTarget.Others);
        }
    }
    [PunRPC]
    void RPC_Interactive()
    {
        animator.SetTrigger("Interactive");
    }
}