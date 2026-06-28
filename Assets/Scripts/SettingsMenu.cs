using UnityEngine;
using UnityEngine.UI;

// Pannello SETTINGS del MainMenu: stessi 3 slider volume della pausa.
// Riusa lo STESSO sistema (AudioManager + PlayerPrefs): nessuna logica audio duplicata,
// i valori restano sincronizzati con la pausa perché passano tutti da AudioManager.
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    public void Open()
    {
        settingsPanel.SetActive(true);
        SeedVolumeSliders();
    }

    public void Close()
    {
        settingsPanel.SetActive(false);
    }

    // Inizializza i 3 slider dai valori salvati senza riscrivere i pref (come PauseMenu).
    void SeedVolumeSliders()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(am.GetMasterVolume());
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(am.GetMusicVolume());
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.GetSFXVolume());
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
