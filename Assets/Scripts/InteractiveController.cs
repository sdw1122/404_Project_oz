using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveController : MonoBehaviour
{
    public Camera playerCamera;         // 플레이어 카메라
    public float interactRange = 3f;    // 상호작용 거리
    public LayerMask interactLayer;     // 상호작용 오브젝트 레이어
    public GameObject interactionUI;    // "상호작용 E" UI 오브젝트
    public Material newMaterial; // Inspector에서 할당

    private InputSystem_Actions interact;
    private bool canInteract = false;
    private RaycastHit lastHit;

    void Awake()
    {
        interact = new InputSystem_Actions();

        interact.Player.Interact.performed += ctx =>
        {
            if (canInteract)
            {
                // 머티리얼 교체
                Renderer renderer = lastHit.collider.GetComponent<Renderer>();
                if (renderer != null && newMaterial != null)
                    renderer.material = newMaterial;

                // isTrigger 활성화
                Collider col = lastHit.collider;
                if (col != null)
                    col.isTrigger = true;
            }
        };
    }

    void OnEnable()
    {
        interact.Enable();
    }

    void OnDisable()
    {
        interact.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            interactionUI.SetActive(true);
            canInteract = true;
            lastHit = hit;
            
        }
        else
        {
            interactionUI.SetActive(false);
        }
    }
}
