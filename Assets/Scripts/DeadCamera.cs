using UnityEngine;

public class DeadCamera : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public float clampAngle = 90f; // 상하 각도 제한 (원하는 값으로 조절)

    private float rotY = 0f; // 위아래 (pitch)
    private float rotX = 0f; // 좌우 (yaw)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.x;
        rotX = rot.y;
        Cursor.lockState = CursorLockMode.Locked; // 마우스 포인터 숨김/락 (필수 아님)
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotX += mouseX;
        rotY -= mouseY;
        rotY = Mathf.Clamp(rotY, -clampAngle, clampAngle);

        // 회전 적용
        transform.localRotation = Quaternion.Euler(rotY, rotX, 0f);
    }
}