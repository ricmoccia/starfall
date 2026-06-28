using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Feedback visibile al click sull'iconcina audio: breve "pop" di scala + lampo di colore,
// POI apre il pannello (SettingsMenu.Open). Il pannello è opaco e copre subito l'icona,
// quindi il pop va mostrato prima dell'apertura. Non modifica SettingsMenu né il pannello.
[RequireComponent(typeof(Image))]
public class IconPressFeedback : MonoBehaviour
{
    [SerializeField] SettingsMenu settingsMenu;
    [SerializeField] float punchScale = 1.3f;
    [SerializeField] float duration = 0.18f;
    [SerializeField] Color flashColor = new Color(0.4f, 1f, 1f, 1f); // ciano chiaro, ben visibile

    Image image;
    Vector3 baseScale;
    Color baseColor;
    Coroutine routine;

    void Awake()
    {
        image = GetComponent<Image>();
        baseScale = transform.localScale;
        baseColor = image.color;
    }

    // Collegato a Button.onClick al posto del vecchio SettingsMenu.Open.
    public void Press()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PressRoutine());
    }

    IEnumerator PressRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // funziona anche se il tempo è fermo
            float k = Mathf.Clamp01(t / duration);
            float pop = Mathf.Sin(k * Mathf.PI);            // 0 -> 1 -> 0
            transform.localScale = baseScale * (1f + (punchScale - 1f) * pop);
            image.color = Color.Lerp(baseColor, flashColor, pop);
            yield return null;
        }
        transform.localScale = baseScale;
        image.color = baseColor;
        routine = null;

        if (settingsMenu != null) settingsMenu.Open();
    }
}
