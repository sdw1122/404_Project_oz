using TMPro;
using UnityEngine;

public class SharedLives : MonoBehaviour
{
    public TextMeshProUGUI shared_lives;
    public int score;

    void Start()
    {
        score = 10;
        UpdateScoreText();
    }

    // GameManager�� ȣ���� ��� ���� �Լ�
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
