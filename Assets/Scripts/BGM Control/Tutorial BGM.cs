using UnityEngine;

public class TutorialBGM : MonoBehaviour
{
    [SerializeField]
    public string bgmName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.instance.PlayBgm(bgmName);
    }
}
