using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] string easyLeaderboardId = "score_easy";
    [SerializeField] string normalLeaderboardId = "score_normal";
    [SerializeField] string hardLeaderboardId = "score_hard";
    [SerializeField] string impossibleLeaderboardId = "score_impossible";

    public struct ScoreRow
    {
        public int Rank;     // 1-based, pronto per il display
        public string Name;  // suffisso #1234 rimosso
        public int Score;
    }

    public static LeaderboardManager Instance { get; private set; }

    // Task dell'invio punteggio in corso: GameOver lo attende prima di leggere la classifica.
    public Task PendingSubmit { get; private set; }

    void Awake()
    {
        ManageSingleton();
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

    public string GetLeaderboardId()
    {
        // Fallback a Normal se non c'è un DifficultyManager (coerente col fallback ×1.0 del gameplay).
        if (DifficultyManager.Instance == null)
        {
            return normalLeaderboardId;
        }
        return GetLeaderboardId(DifficultyManager.Instance.Current);
    }

    public string GetLeaderboardId(DifficultyManager.Difficulty diff)
    {
        switch (diff)
        {
            case DifficultyManager.Difficulty.Easy: return easyLeaderboardId;
            case DifficultyManager.Difficulty.Hard: return hardLeaderboardId;
            case DifficultyManager.Difficulty.Impossible: return impossibleLeaderboardId;
            default: return normalLeaderboardId;
        }
    }

    public async Task<List<ScoreRow>> GetTopScores(DifficultyManager.Difficulty diff, int limit = 10)
    {
        var rows = new List<ScoreRow>();
        string leaderboardId = GetLeaderboardId(diff);
        try
        {
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId, new GetScoresOptions { Limit = limit });
            foreach (LeaderboardEntry entry in page.Results)
            {
                rows.Add(ToRow(entry));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LeaderboardManager: lettura {leaderboardId} fallita: {e}");
        }
        return rows;
    }

    public async Task<ScoreRow?> GetMyEntry(DifficultyManager.Difficulty diff)
    {
        string leaderboardId = GetLeaderboardId(diff);
        try
        {
            LeaderboardEntry entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            return ToRow(entry);
        }
        catch (Exception)
        {
            // Nessuna entry per il giocatore, oppure offline.
            return null;
        }
    }

    static ScoreRow ToRow(LeaderboardEntry entry)
    {
        return new ScoreRow
        {
            Rank = entry.Rank + 1,            // UGS è 0-based
            Name = StripSuffix(entry.PlayerName),
            Score = (int)entry.Score,
        };
    }

    static string StripSuffix(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            return "—";
        }
        return playerName.Split('#')[0];
    }

    // Fire-and-forget: il chiamante (Health.Die) non può await e il player viene distrutto subito dopo.
    // Gira sul manager persistente, quindi la chiamata di rete completa anche durante la transizione a GameOver.
    // Il Task resta esposto in PendingSubmit così GameOver può attenderlo prima di leggere la classifica.
    public void SubmitScore(int score)
    {
        PendingSubmit = SubmitScoreInternal(score);
    }

    async Task SubmitScoreInternal(int score)
    {
        if (GameServicesManager.Instance == null || !GameServicesManager.Instance.IsSignedIn)
        {
            Debug.Log("LeaderboardManager: non loggato, invio punteggio saltato.");
            return;
        }

        string leaderboardId = GetLeaderboardId();
        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            Debug.Log($"LeaderboardManager: punteggio {score} inviato a {leaderboardId}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"LeaderboardManager: invio a {leaderboardId} fallito: {e}");
        }
    }
}
