using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class OptionsUIManager : MonoBehaviour, IUI
{
    [SerializeField] private UIContext uiContext;
    [Header("UI References")]
    public GameObject optionsPanel;
    public Button optionButton;
    public Slider bgmSlider;
    public TMP_Text bgmValueText;
    public Slider sfxSlider;
    public TMP_Text sfxValueText;
    public Toggle bgmToggle;
    public Toggle sfxToggle;
    public Button closeButton;

    private float bgmValue;
    private float sfxValue;
    private bool bgmOn = true;
    private bool sfxOn = true;

    void Awake()
    {
        // 초기 비활성화
        uiContext = uiContext != null ? uiContext : FindAnyObjectByType<UIContext>();
        uiContext?.UIRegistry.Register(this, UIType.PAUSED_MENU);
        optionsPanel.SetActive(false);

        // 저장된 값 불러오기
        bgmValue = PlayerPrefs.GetFloat("BGM_VOLUME", 1f);
        sfxValue = PlayerPrefs.GetFloat("SFX_VOLUME", 1f);
        bgmOn = PlayerPrefs.GetInt("BGM_ON", 1) == 1;
        sfxOn = PlayerPrefs.GetInt("SFX_ON", 1) == 1;

        // UI 초기화
        bgmSlider.value = bgmValue;
        sfxSlider.value = sfxValue;
        bgmToggle.isOn = bgmOn;
        sfxToggle.isOn = sfxOn;
        UpdateValueText();

        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        bgmToggle.onValueChanged.AddListener(OnBGMEnabledChanged);
        sfxToggle.onValueChanged.AddListener(OnSFXEnabledChanged);

        if (AudioManager.Inst)
        {
            AudioManager.Inst.SetBGMVolume(bgmValue);
            AudioManager.Inst.SetSFXVolume(sfxValue);
            AudioManager.Inst.SetBGMEnabled(bgmOn);
            AudioManager.Inst.SetSFXEnabled(sfxOn);
        }
    }
    void OnBGMVolumeChanged(float value)
    {
        bgmValue = value;
        bgmValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        if (AudioManager.Inst)
            AudioManager.Inst.SetBGMVolume(value);
        OnSave();
    }

    void OnSFXVolumeChanged(float value)
    {
        sfxValue = value;
        sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        if (AudioManager.Inst)
        {
            AudioManager.Inst.SetSFXVolume(value);
            AudioManager.Inst.PlaySFX(); // 슬라이더 테스트용 소리
        }
        OnSave();
    }

    void OnBGMEnabledChanged(bool enabled)
    {
        bgmOn = enabled;
        if (AudioManager.Inst)
            AudioManager.Inst.SetBGMEnabled(enabled);
        OnSave();
    }

    void OnSFXEnabledChanged(bool enabled)
    {
        sfxOn = enabled;
        if (AudioManager.Inst)
            AudioManager.Inst.SetSFXEnabled(enabled);
        OnSave();
    }
    void OnSave()
    {
        PlayerPrefs.SetFloat("BGM_VOLUME", bgmValue);
        PlayerPrefs.SetFloat("SFX_VOLUME", sfxValue);
        PlayerPrefs.SetInt("BGM_ON", bgmOn ? 1 : 0);
        PlayerPrefs.SetInt("SFX_ON", sfxOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    private void UpdateValueText()
    {
        bgmValueText.text = $"{Mathf.RoundToInt(bgmValue * 100)}%";
        sfxValueText.text = $"{Mathf.RoundToInt(sfxValue * 100)}%";
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        uiContext.UIRegistry.CloseUI(this);
    }

    public void PositiveInteract(InputAction.CallbackContext context)
    {
        Show();
    }

    public void NegativeInteract(InputAction.CallbackContext context)
    {
        Hide();
    }
}
