using Photon.Pun;
using UnityEngine;

public class PlayerController1 : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    

    Rigidbody rigid;
    PhotonView pv;

    public float JumpPower = 5.0f;
    public float moveSpeed = 5.0f;
    bool isJump;

    public float mouseSensitivity = 100f; // ���콺 ����
    private float xRotation = 0f; // ���� �þ� ����
    public static bool isMove = true;
    public string job;
    [PunRPC]
    public void SetJob(string _job)
    {
        job = _job;
        Debug.Log($"[PlayerController1] Job ������: {job}");
    }
    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        pv= GetComponent<PhotonView>();
        
    }
    private void Start()
    {
        if (!pv.IsMine) return;
        if(playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }
        Cursor.lockState = CursorLockMode.Locked;

    }
    private void FixedUpdate()
    {
        if (pv.IsMine&&isMove) 
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 moveDirection = transform.right * h + transform.forward * v;
            moveDirection.Normalize();

            // Rigidbody.velocity�� ���� ����
            // y�� �ӵ��� �߷¿� ���� ����ǹǷ� ���� �ӵ��� ����
            rigid.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rigid.linearVelocity.y, moveDirection.z * moveSpeed);



            
        }
        if (pv.IsMine)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            // ���� �þ� ȸ�� (ī�޶�)
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // �þ� ���� ���� (������ �ʹ� �ڷ� �����ų� ������ �ʵ���)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // �¿� �þ� ȸ�� (�÷��̾� ��ü�� �Բ�)
            transform.Rotate(Vector3.up * mouseX);
        }
        
            
        

    }

    private void Update()
    {   if(pv.IsMine&&isMove) 
        {
            if (Input.GetButtonDown("Jump") && !isJump)
            {
                isJump = true;
                rigid.AddForce(new Vector3(0, JumpPower, 0), ForceMode.Impulse);
            }
        }
       
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (pv.IsMine) 
        {
            if (collision.gameObject.name == "Plane")
                isJump = false;
        }
       
    }
    [PunRPC]
    void SendMyDataToHost()
    {
        if (!pv.IsMine) return;
        
        PlayerSaveData myData = new PlayerSaveData
        {
            userId = PhotonNetwork.LocalPlayer.UserId,
            userJob=job,
            position = transform.position,
            // ���߿� ������ ������ �߰�
        };
        GameObject gm = GameObject.Find("GameManager");
        PhotonView gmView = gm.GetComponent<PhotonView>();

        //gm���� ������ ����
        string json = JsonUtility.ToJson(myData);
        gmView.RPC("ReceivePlayerData", RpcTarget.MasterClient, json);
    }
    


}
