using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;
using static LobbyManager;

public class InteractiveController : MonoBehaviour
{
    public Camera playerCamera;         // 플레이어 카메라
    public float interactRange = 3f;    // 상호작용 거리
    public LayerMask interactLayer;     // 상호작용 오브젝트 레이어
    public GameObject UI; //interact UI
    private GameObject UIObject; //가져온 UI
    public Material newMaterial; // Inspector에서 할당
    
    private bool canInteract = false;
    private RaycastHit lastHit;
    string job = TempMemory.MySaveData != null ? TempMemory.MySaveData.userJob : "pen";

    void Awake()
    {
        UIObject = Instantiate(UI);
        UIObject.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (canInteract)
        {
            if (job == "eraser")
            {
                Eraser();
            }
            else if (job == "pen")
            {
                Pen();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            UIObject.SetActive(true);
            canInteract = true;
            lastHit = hit;            
        }
        else
        {
            UIObject.SetActive(false);
        }
    }

    void Eraser()
    {
        // 머티리얼 교체
        //Transform temp = lastHit.collider.transform.Find("temp");
        Transform temp = lastHit.collider.transform;
        if (temp != null)
        {
            Renderer tempRenderer = temp.transform.GetComponent<Renderer>();
            if (tempRenderer != null && newMaterial != null)
            {
                tempRenderer.material = newMaterial;
            }
        }

        // isTrigger 활성화
        Collider col = lastHit.collider;
        GameObject hitObject = lastHit.collider.gameObject;
        Rigidbody rb = hitObject.GetComponent<Rigidbody>();      
        col.isTrigger = true;
    }

    void Pen()
    {
        // 머티리얼 교체
        //Transform temp = lastHit.collider.transform.Find("temp");
        Transform temp = lastHit.collider.transform;
        if (temp != null)
        {
            Renderer tempRenderer = temp.transform.GetComponent<Renderer>();
            if (tempRenderer != null && newMaterial != null)
            {
                tempRenderer.material = newMaterial;
            }
        }

        // isTrigger 비활성화
        Collider col = lastHit.collider;
        GameObject hitObject = lastHit.collider.gameObject;
        Rigidbody rb = hitObject.GetComponent<Rigidbody>();
        col.isTrigger = false;
    }
}
