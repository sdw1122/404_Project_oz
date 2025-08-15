using TMPro;
using UnityEngine;

public class SharedLives : MonoBehaviour
{
    public TextMeshProUGUI shared_lives;
    public int score = 10; // 초기값 설정

    void Start()
    {
        UpdateScoreText();
    }

    // GameManager의 RPC가 호출할 목숨 감소 함수
    public void LoseLife()
    {
        if (score > 0)
        {
            score -= 1;
            UpdateScoreText();
        }
    }

    void UpdateScoreText()
    {
        shared_lives.text = "X " + score;
    }
}
