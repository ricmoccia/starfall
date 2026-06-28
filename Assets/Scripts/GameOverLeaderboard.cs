using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

public class GameOverLeaderboard : MonoBehaviour
{
    [SerializeField] Transform contentParent;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text headerText;
    [SerializeField] TMP_FontAsset rowFont;
    [SerializeField] Color highlightColor = new Color(0.784f, 0.118f, 1f, 1f);
    [SerializeField] int limit = 8;

    async void Start()
    {
        statusText.text = "Loading…";

        var lm = LeaderboardManager.Instance;
        if (lm == null)
        {
            statusText.text = "Leaderboard unavailable.";
            return;
        }

        try
        {
            // Attende l'invio del punteggio appena fatto (partito alla morte), così è incluso nella lettura.
            // Con timeout: se l'invio di rete è lento/bloccato, si procede comunque a leggere la classifica.
            if (lm.PendingSubmit != null)
            {
                await Task.WhenAny(lm.PendingSubmit, Task.Delay(3000));
            }

            var diff = DifficultyManager.Instance != null
                ? DifficultyManager.Instance.Current
                : DifficultyManager.Difficulty.Normal;

            if (headerText != null)
            {
                headerText.text = "LEADERBOARD — " + diff.ToString().ToUpper();
            }

            List<LeaderboardManager.ScoreRow> rows = await lm.GetTopScores(diff, limit);
            LeaderboardManager.ScoreRow? mine = await lm.GetMyEntry(diff);

            if (rows.Count == 0)
            {
                LeaderboardRowRenderer.Clear(contentParent);
                statusText.text = "No scores yet.";
                return;
            }

            statusText.text = "";
            LeaderboardRowRenderer.Render(contentParent, rows, mine, rowFont, highlightColor);
        }
        catch
        {
            statusText.text = "Leaderboard unavailable.";
        }
    }
}
