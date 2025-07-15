using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;
using static LobbyManager;
using Photon.Pun;

public class InteractiveController : MonoBehaviour
{
    PhotonView pv;
    public Camera playerCamera;         // 플레이어 카메라
    public float interactRange = 3f;    // 상호작용 거리
    public LayerMask interactLayer;     // 상호작용 오브젝트 레이어
    public GameObject UI; //interact UI
    private GameObject UIObject; //가져온 UI
    public Material[] newMaterial; // Inspector에서 할당    

    private bool canInteract = false;
    private RaycastHit lastHit;
    string job = TempMemory.MySaveData != null ? TempMemory.MySaveData.userJob : "pen";

    void Awake()
    {
        pv = GetComponent<PhotonView>();
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
        //Rigidbody rb = GetComponent<Rigidbody>();
        //if (rb != null)
        //{
        //    rb.MovePosition(new Vector3(14f, 52f, -12.5f));
        //    Debug.Log("상호");
        //}
        //else
        //{
        //    transform.position = new Vector3(11f, 1f, -12.5f);
        //}
    }

    // Update is called once per frame
    void Update()
    {        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer) && pv.IsMine)
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

    [PunRPC]
    public void ObjectInteract(int viewID, int materialIndex, bool triggerOn)
    {
        // 머티리얼 교체
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView == null) return;

        GameObject target = targetView.gameObject;
        Renderer rend = target.GetComponent<Renderer>();

        if (rend != null)
        {
            rend.material = newMaterial[materialIndex];
        }

        Collider col = lastHit.collider;
        if (col != null)
        {
            col.isTrigger = triggerOn;
        }
    }

    void Eraser()
    {
        GameObject targetObject = lastHit.collider.gameObject;
        PhotonView targetPV = targetObject.GetComponent<PhotonView>();
        if (targetPV == null) return;

        pv.RPC("ObjectInteract", RpcTarget.All, targetPV.ViewID, 0, true);
     }

    void Pen()
    {
        GameObject targetObject = lastHit.collider.gameObject;
        PhotonView targetPV = targetObject.GetComponent<PhotonView>();
        if (targetPV == null) return;

        pv.RPC("ObjectInteract", RpcTarget.All, targetPV.ViewID, 1, false);
    }
}
