using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// BGM 정보를 담을 클래스
[System.Serializable]
public class BgmSound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 0.5f;
}

// SFX 정보를 담을 클래스 (기존 Sound 클래스에서 확장)
[System.Serializable]
public class SfxSound
{
    public string name;
    public AudioClip clip;

    [Header("Sound Settings")]
    [Range(0f, 1f)]
    public float volume = 0.7f;
    [Range(0.1f, 3f)]
    public float pitch = 1f; // 피치 조절 추가

    [Header("3D Sound Settings")]
    [Range(0f, 50f)]
    public float minDistance = 1f;
    [Range(0f, 500f)]
    public float maxDistance = 100f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    // --- 사운드 데이터 ---
    public BgmSound[] bgmSounds;
    public SfxSound[] sfxSounds;

    // --- 플레이어 ---
    public AudioSource bgmPlayer;
    private List<AudioSource> sfxPool;
    public int poolSize = 15;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeSfxPool();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region BGM
    public void PlayBgm(string name)
    {
        BgmSound sound = Array.Find(bgmSounds, s => s.name == name);
        if (sound != null)
        {
            bgmPlayer.clip = sound.clip;
            bgmPlayer.volume = sound.volume;
            bgmPlayer.Play();
        }
        else
        {
            Debug.LogWarning("AudioManager: BGM not found with name: " + name);
        }
    }
    #endregion

    #region SFX
    public void PlaySfxAtLocation(string name, Vector3 position)
    {
        SfxSound sound = Array.Find(sfxSounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning("AudioManager: SFX not found with name: " + name);
            return;
        }

        AudioSource source = GetAvailableSfxPlayer();
        if (source != null)
        {
            source.transform.position = position;
            
            // SfxSound 객체에 저장된 모든 값 적용
            source.clip = sound.clip;
            source.volume = sound.volume;
            source.pitch = sound.pitch; // 피치 적용
            source.minDistance = sound.minDistance;
            source.maxDistance = sound.maxDistance;

            source.gameObject.SetActive(true);
            source.Play();
            StartCoroutine(ReturnToPool(source.gameObject, sound.clip.length)); // 피치에 따라 재생시간 변경
        }
    }
    #endregion

    #region Pooling
    void InitializeSfxPool()
    {
        sfxPool = new List<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject sfxObject = new GameObject("SFXPlayer_" + i);
            sfxObject.transform.SetParent(this.transform);
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1.0f;
            sfxObject.SetActive(false);
            sfxPool.Add(source);
        }
    }

    AudioSource GetAvailableSfxPlayer()
    {
        foreach (var source in sfxPool)
        {
            if (!source.gameObject.activeInHierarchy)
            {
                return source;
            }
        }
        // 풀이 부족할 경우 새로 생성 (안정성 강화)
        GameObject newSfxObject = new GameObject("SFXPlayer_" + (poolSize++));
        newSfxObject.transform.SetParent(this.transform);
        AudioSource newSource = newSfxObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.spatialBlend = 1.0f;
        sfxPool.Add(newSource); // 새로 만든 객체도 풀에 추가
        return newSource;
    }

    IEnumerator ReturnToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
    #endregion
}