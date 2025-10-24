using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Inst;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    private void Awake()
    {
        if (Inst != null)
        {
            Destroy(gameObject);
            return;
        }

        Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetBGMVolume(float value)
    {
        if (bgmSource)
            bgmSource.volume = value;
    }

    public void SetSFXVolume(float value)
    {
        if (sfxSource)
            sfxSource.volume = value;
    }

    public void SetBGMEnabled(bool enabled)
    {
        if (bgmSource)
            bgmSource.mute = !enabled;
    }

    public void SetSFXEnabled(bool enabled)
    {
        if (sfxSource)
            sfxSource.mute = !enabled;
    }

    // 테스트용 효과음 재생
    public void PlaySFX()
    {
        if (sfxSource && !sfxSource.mute)
            sfxSource.Play();
    }
}
