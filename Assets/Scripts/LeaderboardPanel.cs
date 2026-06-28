using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform contentParent;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_FontAsset rowFont;
    [SerializeField] Color highlightColor = new Color(0.784f, 0.118f, 1f, 1f);

    [Header("Difficulty Tabs")]
    [SerializeField] Image tabEasy;
    [SerializeField] Image tabNormal;
    [SerializeField] Image tabHard;
    [SerializeField] Image tabImpossible;
    [SerializeField] Color tabInactiveColor = new Color(0.16f, 0.16f, 0.22f, 1f);

    public void Open()
    {
        panelRoot.SetActive(true);
        ShowNormal();
    }

    public void Close()
    {
        panelRoot.SetActive(false);
    }

    public void ShowEasy() { Load(DifficultyManager.Difficulty.Easy); }
    public void ShowNormal() { Load(DifficultyManager.Difficulty.Normal); }
    public void ShowHard() { Load(DifficultyManager.Difficulty.Hard); }
    public void ShowImpossible() { Load(DifficultyManager.Difficulty.Impossible); }

    async void Load(DifficultyManager.Difficulty diff)
    {
        // Evidenzia subito il tab attivo (prima delle await) così cambia all'istante.
        UpdateTabHighlight(diff);

        LeaderboardRowRenderer.Clear(contentParent);
        statusText.text = "Loading…";

        if (LeaderboardManager.Instance == null)
        {
            statusText.text = "Leaderboard unavailable.";
            return;
        }

        List<LeaderboardManager.ScoreRow> rows = await LeaderboardManager.Instance.GetTopScores(diff);
        LeaderboardManager.ScoreRow? mine = await LeaderboardManager.Instance.GetMyEntry(diff);

        // Il pannello potrebbe essere stato chiuso o un altro tab caricato nel frattempo.
        if (!panelRoot.activeSelf)
        {
            return;
        }

        if (rows.Count == 0)
        {
            LeaderboardRowRenderer.Clear(contentParent);
            statusText.text = "No scores yet.";
            return;
        }

        statusText.text = "";
        LeaderboardRowRenderer.Render(contentParent, rows, mine, rowFont, highlightColor);
    }

    // Solo aspetto visivo: tab attivo con accent #C81EFF, gli altri due spenti.
    void UpdateTabHighlight(DifficultyManager.Difficulty diff)
    {
        SetTab(tabEasy, diff == DifficultyManager.Difficulty.Easy);
        SetTab(tabNormal, diff == DifficultyManager.Difficulty.Normal);
        SetTab(tabHard, diff == DifficultyManager.Difficulty.Hard);
        SetTab(tabImpossible, diff == DifficultyManager.Difficulty.Impossible);
    }

    void SetTab(Image tab, bool active)
    {
        if (tab == null) return;
        // Disabilita il ColorTint del Button così non sovrascrive il colore impostato a mano.
        Button btn = tab.GetComponent<Button>();
        if (btn != null) btn.transition = Selectable.Transition.None;
        tab.color = active ? highlightColor : tabInactiveColor;
    }
}
