using System.Collections;
using UnityEngine;
using TMPro;

// Messaggio "BOSS DEFEATED +N" che appare e sfuma dentro il container SafeArea.
// Non blocca il gioco (niente timeScale): usa il tempo reale.
public class BossDefeatMessage : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text text;
    [SerializeField] float fadeIn = 0.3f;
    [SerializeField] float hold = 1.5f;
    [SerializeField] float fadeOut = 1.2f;

    Coroutine routine;

    void Awake()
    {
        if (group != null)
        {
            group.alpha = 0f;
            // Banner passivo: non deve MAI intercettare i tocchi (anche da invisibile),
            // altrimenti copre lo slider OVERALL in pausa. Si anima solo l'alpha.
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    public void Show(string message)
    {
        if (text != null) text.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        yield return Fade(0f, 1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f, fadeOut);
        routine = null;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
