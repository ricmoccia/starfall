using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource sfxSource;

    [Header("Shooting SFX")]
    [SerializeField] AudioClip shootingClip;
    [SerializeField][Range(0, 1)] float shootingVolume = 1f;

    [Header("Damage SFX")]
    [SerializeField] AudioClip damageClip;
    [SerializeField][Range(0, 1)] float damageVolume = 1f;

    [Header("Enemy Explosion SFX")]
    [SerializeField] AudioClip enemyExplosionClip;
    [SerializeField][Range(0, 1)] float enemyExplosionVolume = 0.35f;

    [Header("Player Death SFX")]
    [SerializeField] AudioClip playerDeathClip;
    [SerializeField][Range(0, 1)] float playerDeathVolume = 0.6f;

    [Header("Power-up Pickup SFX")]
    [SerializeField] AudioClip pickupClip;
    [SerializeField][Range(0, 1)] float pickupVolume = 0.45f;

    [Header("UI Click SFX")]
    [SerializeField] AudioClip uiClickClip;
    [SerializeField][Range(0, 1)] float uiClickVolume = 0.3f;

    public static AudioManager Instance { get; private set; }

    // Parametri esposti dal GameMixer (gruppi Master -> Music, SFX).
    const string MASTER_PARAM = "MasterVol";
    const string MUSIC_PARAM = "MusicVol";
    const string SFX_PARAM = "SFXVol";

    // Chiavi PlayerPrefs (valori 0-1). "MasterVolume" mantenuto per retrocompatibilità.
    const string PREF_MASTER = "MasterVolume";
    const string PREF_MUSIC = "MusicVolume";
    const string PREF_SFX = "SFXVolume";

    void Awake()
    {
        ManageSingleton();
        // Tentativo in Awake; ripetuto in Start perché SetFloat su un parametro esposto
        // può non avere effetto nel primo frame in cui il mixer viene caricato.
        if (Instance == this)
        {
            LoadAndApplyVolumes();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (Instance == this)
        {
            LoadAndApplyVolumes();
            HookButtons(); // scena d'origine (sceneLoaded non scatta per la scena già caricata)
        }
    }

    // Ad ogni cambio scena ricollega il click ai bottoni: le istanze precedenti sono
    // distrutte col cambio scena, quindi niente listener duplicati.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance == this)
        {
            HookButtons();
        }
    }

    void HookButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            b.onClick.AddListener(PlayUIClick);
        }
    }

    void ManageSingleton()
    {
        if (Instance != null)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void LoadAndApplyVolumes()
    {
        ApplyVolume(MASTER_PARAM, GetMasterVolume());
        ApplyVolume(MUSIC_PARAM, GetMusicVolume());
        ApplyVolume(SFX_PARAM, GetSFXVolume());
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat(PREF_MASTER, 1f);
    public float GetMusicVolume() => PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(PREF_SFX, 1f);

    public void SetMasterVolume(float v) { ApplyVolume(MASTER_PARAM, v); PlayerPrefs.SetFloat(PREF_MASTER, v); }
    public void SetMusicVolume(float v) { ApplyVolume(MUSIC_PARAM, v); PlayerPrefs.SetFloat(PREF_MUSIC, v); }
    public void SetSFXVolume(float v) { ApplyVolume(SFX_PARAM, v); PlayerPrefs.SetFloat(PREF_SFX, v); }

    void ApplyVolume(string param, float linear)
    {
        if (mixer != null)
        {
            mixer.SetFloat(param, LinearToDb(linear));
        }
    }

    // 0 -> -80 dB (silenzio), 1 -> 0 dB. Log: la percezione del volume è logaritmica.
    static float LinearToDb(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }

    public void PlayShootingSFX() { PlaySFX(shootingClip, shootingVolume); }
    public void PlayDamageSFX() { PlaySFX(damageClip, damageVolume); }
    public void PlayEnemyExplosion() { PlaySFX(enemyExplosionClip, enemyExplosionVolume); }
    public void PlayPlayerDeath() { PlaySFX(playerDeathClip, playerDeathVolume); }
    public void PlayPickup() { PlaySFX(pickupClip, pickupVolume); }
    public void PlayUIClick() { PlaySFX(uiClickClip, uiClickVolume); }

    // Metodo generico: instradato al gruppo SFX del mixer (output dell'sfxSource).
    // PlayOneShot consente sovrapposizioni senza tagliare i suoni in corso.
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
