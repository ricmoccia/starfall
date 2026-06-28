using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Sta sul Canvas (sempre attivo): un oggetto inattivo non eseguirebbe Update e non
// potrebbe riaprire la pausa. Mostra/nasconde il pannello via SetActive.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    bool isPaused;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Click UI per la pausa/ripresa da tastiera (i bottoni Pause/Resume lo ricevono dall'auto-hook).
            AudioManager.Instance?.PlayUIClick();
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        SeedVolumeSliders();
    }

    // Inizializza i 3 slider dai valori salvati senza riscrivere i pref.
    void SeedVolumeSliders()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(am.GetMasterVolume());
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(am.GetMusicVolume());
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.GetSFXVolume());
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void LoadMainMenu()
    {
        // Prima sblocca il tempo, poi cambia scena: altrimenti il menu resterebbe congelato.
        Time.timeScale = 1f;
        FindFirstObjectByType<LevelManager>().LoadMainMenu();
    }

    public void QuitGame()
    {
        // Sblocca il tempo e torna al main menu (niente uscita dall'app dalla pausa).
        Time.timeScale = 1f;
        FindFirstObjectByType<LevelManager>().LoadMainMenu();
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }
}
