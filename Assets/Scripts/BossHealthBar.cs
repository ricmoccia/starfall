using UnityEngine;
using UnityEngine.UI;

// Barra vita del boss in cima allo schermo (dentro il container SafeArea).
// Il componente sta su un oggetto sempre attivo; mostra/nasconde il solo contenuto visibile.
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] GameObject bar;   // contenuto visibile (slider + etichetta), nascosto di default
    [SerializeField] Slider slider;

    public void Show()
    {
        if (bar != null) bar.SetActive(true);
    }

    public void Hide()
    {
        if (bar != null) bar.SetActive(false);
    }

    public void SetFraction(float fraction)
    {
        if (slider != null) slider.value = Mathf.Clamp01(fraction);
    }
}
