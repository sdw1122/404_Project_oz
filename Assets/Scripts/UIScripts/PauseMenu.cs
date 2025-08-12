using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using UnityEngine.SceneManagement;

// MonoBehaviourPunCallbacks를 상속받습니다.
public class PauseMenu : MonoBehaviourPunCallbacks
{
    public static bool IsPaused = false;

    [Tooltip("Hierarchy에 있는 PauseMenuPanel을 연결하세요.")]
    public GameObject pauseMenuUI;

    private PlayerController localPlayerController;
    private PlayerInput localPlayerInput;
    private bool isPlayerFound = false;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Start()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPlayerFound)
            {
                FindLocalPlayer();
            }

            if (isPlayerFound)
            {
                if (IsPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
    }

    private void FindLocalPlayer()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in allPlayers)
        {
            if (player.GetComponent<PhotonView>().IsMine)
            {
                localPlayerController = player;
                localPlayerInput = player.GetComponent<PlayerInput>();
                isPlayerFound = true;
                return;
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (localPlayerController != null) localPlayerController.canMove = true;
        if (localPlayerInput != null) localPlayerInput.ActivateInput();
        IsPaused = false;
    }

    void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (localPlayerController != null)
        {
            localPlayerController.canMove = false;
            localPlayerController.ResetMoveInput();
        }
        if (localPlayerInput != null) localPlayerInput.DeactivateInput();
        IsPaused = true;
    }

    /// <summary>
    /// [수정됨] 게임 애플리케이션을 완전히 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다...");

        // 빌드된 게임에서만 작동합니다.
        Application.Quit();

        // 유니티 에디터에서 테스트할 때 종료되도록 하는 코드입니다.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // OnLeftRoom 콜백은 이제 사용하지 않지만, 다른 곳에서 필요할 수 있으니 남겨둡니다.
    public override void OnLeftRoom()
    {
        // 로비 씬으로 이동하는 로직이 필요할 경우 여기에 작성합니다.
        // SceneManager.LoadScene("Lobby");
    }
}