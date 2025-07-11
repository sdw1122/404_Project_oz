using UnityEngine;
using UnityEngine.SceneManagement; // 씬을 다시 로드하기 위해 추가

public class PJS_GameManager : MonoBehaviour
{
    // 다른 스크립트에서 GameManager를 쉽게 참조할 수 있도록 만드는 싱글톤 패턴
    public static PJS_GameManager Instance;

    public HealthBar healthBar;       // Inspector에서 HealthBar 스크립트 연결
    public SharedLives sharedLives;   // Inspector에서 SharedLives 스크립트 연결

    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            // P 키를 눌렀을 때 플레이어의 체력을 10 감소시키는 테스트용 코드
            healthBar.TakeDamage(10);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            healthBar.ResetHealth();
        }
    }


    // 플레이어의 체력이 0이 되었을 때 호출될 함수
    public void PlayerDied()
    {
        // 목숨(SharedLives)을 1 감소시킴
        sharedLives.LoseLife();

        // 남은 목숨이 있는지 확인
        if (sharedLives.score > 0)
        {
            // 목숨이 남아있다면, 체력을 다시 채움
            healthBar.ResetHealth();
        }
        else
        {
            // 목숨이 없다면, 게임오버 처리
            GameOver();
        }
    }

    // 게임오버 처리 함수
    void GameOver()
    {
        Debug.Log("게임 오버!");
        // 여기에 게임오버 UI를 띄우거나, 현재 씬을 다시 시작하는 등의 로직을 추가할 수 있습니다.
        // 예: SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
