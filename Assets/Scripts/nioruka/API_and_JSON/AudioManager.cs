using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public SerializedDictionary<string, AudioClip> sfxSources = new();
    public SerializedDictionary<string, AudioClip> musicSources = new();
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
        
        SetBGMEnabled(PlayerPrefs.GetInt("BGM_ON") == 1);
        SetSFXEnabled(PlayerPrefs.GetInt("SFX_ON") == 1);
        SetBGMVolume(PlayerPrefs.GetFloat("BGM_Volume"));
        SetSFXVolume(PlayerPrefs.GetFloat("SFX_Volume"));
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

    public void PlaySFX(string sfxName)
    {
        sfxSources.TryGetValue(sfxName, out AudioClip clip);
        if (clip == null) return;
        Debug.Log(clip.name);
        sfxSource.PlayOneShot(clip);
    }
}
