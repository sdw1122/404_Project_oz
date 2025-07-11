using UnityEngine;
using UnityEngine.SceneManagement; // ���� �ٽ� �ε��ϱ� ���� �߰�

public class PJS_GameManager : MonoBehaviour
{
    // �ٸ� ��ũ��Ʈ���� GameManager�� ���� ������ �� �ֵ��� ����� �̱��� ����
    public static PJS_GameManager Instance;

    public HealthBar healthBar;       // Inspector���� HealthBar ��ũ��Ʈ ����
    public SharedLives sharedLives;   // Inspector���� SharedLives ��ũ��Ʈ ����

    void Awake()
    {
        // �̱��� �ν��Ͻ� ����
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
            // P Ű�� ������ �� �÷��̾��� ü���� 10 ���ҽ�Ű�� �׽�Ʈ�� �ڵ�
            healthBar.TakeDamage(10);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            healthBar.ResetHealth();
        }
    }


    // �÷��̾��� ü���� 0�� �Ǿ��� �� ȣ��� �Լ�
    public void PlayerDied()
    {
        // ���(SharedLives)�� 1 ���ҽ�Ŵ
        sharedLives.LoseLife();

        // ���� ����� �ִ��� Ȯ��
        if (sharedLives.score > 0)
        {
            // ����� �����ִٸ�, ü���� �ٽ� ä��
            healthBar.ResetHealth();
        }
        else
        {
            // ����� ���ٸ�, ���ӿ��� ó��
            GameOver();
        }
    }

    // ���ӿ��� ó�� �Լ�
    void GameOver()
    {
        Debug.Log("���� ����!");
        // ���⿡ ���ӿ��� UI�� ���ų�, ���� ���� �ٽ� �����ϴ� ���� ������ �߰��� �� �ֽ��ϴ�.
        // ��: SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
