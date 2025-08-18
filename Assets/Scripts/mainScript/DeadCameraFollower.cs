using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeadCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 originalPos;
    public GameObject playerObj;
    Vector3 targetPos;
    bool isViewingPartner = false;
    private void Awake()
    {

        PhotonView[] allViews = FindObjectsByType<PhotonView>(default);
        foreach (PhotonView view in allViews)
        {
            if (view.IsMine) continue; // 내꺼 제외

            PlayerHealth otherHealth = view.GetComponent<PlayerHealth>();
            if (otherHealth != null)
            {
                target = otherHealth.transform;
                Debug.Log($"[DeadCamera] 타겟 플레이어: {otherHealth.name}");
                break;
            }
        }
    }


    void Update()
    {
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            ToggleCameraView();
        }
        Vector3 parentPos = playerObj.transform.position;
        originalPos = new Vector3(parentPos.x, parentPos.y + 3f, parentPos.z);
        targetPos =new Vector3(target.position.x-1f, target.position.y +3f,target.position.z);
        // 이동 처리
        transform.position = Vector3.Lerp(transform.position, isViewingPartner ? targetPos : originalPos, Time.deltaTime * 5f);

    }

    void ToggleCameraView()
    {
        isViewingPartner = !isViewingPartner;
        Debug.Log($"[DeadCamera] 시점 전환됨 → {(isViewingPartner ? "다른 플레이어" : "내 시체")}");
    }
}