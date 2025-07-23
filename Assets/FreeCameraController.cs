using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FreeCameraController : MonoBehaviour
{
    [Header("�̵� �ӵ�")]
    public float moveSpeed = 10f;
    public float boostMultiplier = 3f;

    [Header("���콺 ����")]
    public float lookSpeed = 0.1f;
    public float lookXLimit = 80f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var angles = transform.localEulerAngles;
        rotationY = angles.y;
        rotationX = angles.x;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // ���콺 ȸ��
        Vector2 delta = mouse.delta.ReadValue();
        rotationY += delta.x * lookSpeed;
        rotationX -= delta.y * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);

        // �̵� �Է�
        Vector3 dir = Vector3.zero;
        if (keyboard.wKey.isPressed) dir += transform.forward;
        if (keyboard.sKey.isPressed) dir -= transform.forward;
        if (keyboard.dKey.isPressed) dir += transform.right;
        if (keyboard.aKey.isPressed) dir -= transform.right;
        if (keyboard.eKey.isPressed) dir += transform.up;
        if (keyboard.qKey.isPressed) dir -= transform.up;

        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? boostMultiplier : 1f);
        transform.position += dir.normalized * speed * Time.deltaTime;

        // ESC ������ Ŀ�� Ǯ��
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
