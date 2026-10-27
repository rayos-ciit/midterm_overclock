using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AudioShakeManager : MonoBehaviour
{
    #region Audio UI & Settings
    [Header("Audio UI Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip enemyDeathClip;
    [SerializeField] private AudioClip baseDamageClip;
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip overclockClip;
    [SerializeField] private AudioClip overheatClip;

    [Header("Audio Clips Volume")]
    [Range(0f, 1f)] [SerializeField] private float shootVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float enemyDeathVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float baseDamageVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float uiClickVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float winVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float loseVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float overclockVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float overheatVolume = 0.8f;

    #endregion

    #region State Variables
    private AudioSource audioSrc;
    private Camera mainCam;
    private Vector3 cameraOriginalPos;
    
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.2f;
    #endregion

    #region Initialization
    private void Awake()
    {
        audioSrc = GetComponent<AudioSource>();
        mainCam = Camera.main;
        
        if (mainCam != null) 
        {
            cameraOriginalPos = mainCam.transform.position;
        }

        GenerateMissingSynths();
        InitializeUIBindings();
    }

    private void GenerateMissingSynths()
    {
        if (shootClip == null)      shootClip      = CreateSynthClip("Shoot", 680f, 180f, 0.09f, false);
        if (enemyDeathClip == null) enemyDeathClip = CreateSynthClip("Death", 320f, 70f, 0.16f, true);
        if (baseDamageClip == null) baseDamageClip = CreateSynthClip("BaseHit", 160f, 45f, 0.30f, true);
        if (uiClickClip == null)    uiClickClip    = CreateSynthClip("UIClick", 880f, 1200f, 0.05f, false);
        if (winClip == null)        winClip        = CreateSynthClip("Win", 440f, 880f, 0.45f, false);
        if (loseClip == null)       loseClip       = CreateSynthClip("Lose", 300f, 90f, 0.55f, false);
    }

    private void InitializeUIBindings()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        if (muteToggle != null)
        {
            muteToggle.isOn = AudioListener.pause;
            muteToggle.onValueChanged.AddListener(SetMute);
        }
    }
    #endregion

    #region Unity Lifecycle (Camera Shake)
    private void Update()
    {
        // Guard Clause
        if (mainCam == null || shakeTimer <= 0f) return;

        shakeTimer -= Time.unscaledDeltaTime;
        
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * shakeMagnitude;
        mainCam.transform.position = cameraOriginalPos + offset;

        if (shakeTimer <= 0f)
        {
            mainCam.transform.position = cameraOriginalPos;
        }
    }
    #endregion

    #region Public Service Methods
    public void TriggerScreenShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    public void SetVolume(float val) => AudioListener.volume = Mathf.Clamp01(val);
    public void SetMute(bool muted) => AudioListener.pause = muted;

    public void PlayShoot() => PlaySFX(shootClip, shootVolume);
    public void PlayEnemyDeath() => PlaySFX(enemyDeathClip, enemyDeathVolume);
    public void PlayBaseDamage() => PlaySFX(baseDamageClip, baseDamageVolume);
    public void PlayUIClick(float vol = -1f) => PlaySFX(uiClickClip, vol >= 0f ? vol : uiClickVolume);
    public void PlayWin() => PlaySFX(winClip, winVolume);
    public void PlayLose() => PlaySFX(loseClip, loseVolume);
    public void PlayOverclock() => PlaySFX(overclockClip, overclockVolume);
    public void PlayOverheat() => PlaySFX(overheatClip, overheatVolume);

    public void PlaySFX(AudioClip clip, float vol = 0.7f)
    {
        if (clip != null && audioSrc != null && !AudioListener.pause)
        {
            audioSrc.PlayOneShot(clip, vol);
        }
    }
    #endregion

    #region Procedural Audio Generation
    private AudioClip CreateSynthClip(string clipName, float startFreq, float endFreq, float duration, bool addNoise)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += (freq * 2f * Mathf.PI) / sampleRate;

            float wave = Mathf.Sin(phase);
            if (addNoise) wave = Mathf.Lerp(wave, Random.Range(-1f, 1f), 0.35f);

            float envelope = 1f - t;
            samples[i] = wave * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}