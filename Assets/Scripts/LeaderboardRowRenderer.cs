using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Rendering condiviso delle righe di classifica (usato da LeaderboardPanel e GameOverLeaderboard).
public static class LeaderboardRowRenderer
{
    public static void Clear(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(content.GetChild(i).gameObject);
        }
    }

    public static void Render(Transform content, IList<LeaderboardManager.ScoreRow> rows,
        LeaderboardManager.ScoreRow? mine, TMP_FontAsset font, Color highlightColor)
    {
        Clear(content);

        bool mineShown = false;
        foreach (var row in rows)
        {
            bool isMine = mine.HasValue && row.Rank == mine.Value.Rank;
            if (isMine) mineShown = true;
            CreateRow(content, row.Rank.ToString(), row.Name, row.Score.ToString(), isMine, font, highlightColor);
        }

        // Giocatore fuori dalla top-N: separatore + la sua riga reale, evidenziata.
        if (mine.HasValue && !mineShown)
        {
            CreateSeparator(content, font);
            var me = mine.Value;
            CreateRow(content, me.Rank.ToString(), me.Name, me.Score.ToString(), true, font, highlightColor);
        }

        // Forza il rebuild del layout: aggiungendo righe in una continuation async, il
        // VerticalLayoutGroup non le ridimensiona da solo → righe a larghezza errata (invisibili).
        var rt = content as RectTransform;
        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    static void CreateSeparator(Transform content, TMP_FontAsset font)
    {
        var rowGo = new GameObject("Separator", typeof(RectTransform));
        rowGo.transform.SetParent(content, false);

        rowGo.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        var le = rowGo.AddComponent<LayoutElement>();
        le.minHeight = 40;

        MakeCell(rowGo.transform, "· · ·", 0, TextAlignmentOptions.Center, 1, new Color(0.5f, 0.5f, 0.55f), font);
    }

    static void CreateRow(Transform content, string rank, string name, string score, bool highlight,
        TMP_FontAsset font, Color highlightColor)
    {
        var rowGo = new GameObject("Row", typeof(RectTransform));
        rowGo.transform.SetParent(content, false);

        var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.spacing = 16;
        layout.padding = new RectOffset(20, 20, 6, 6);

        var le = rowGo.AddComponent<LayoutElement>();
        le.minHeight = 64;

        Color col = highlight ? highlightColor : Color.white;
        MakeCell(rowGo.transform, rank, 90, TextAlignmentOptions.MidlineLeft, 0, col, font);
        MakeCell(rowGo.transform, name, 0, TextAlignmentOptions.MidlineLeft, 1, col, font);
        MakeCell(rowGo.transform, score, 200, TextAlignmentOptions.MidlineRight, 0, col, font);
    }

    static void MakeCell(Transform parent, string text, float preferredWidth, TextAlignmentOptions align,
        float flexibleWidth, Color col, TMP_FontAsset font)
    {
        var cell = new GameObject("Cell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);

        var t = cell.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = 34;
        t.color = col;
        t.alignment = align;
        t.enableAutoSizing = false;
        t.overflowMode = TextOverflowModes.Overflow;

        var le = cell.AddComponent<LayoutElement>();
        if (preferredWidth > 0) le.preferredWidth = preferredWidth;
        le.flexibleWidth = flexibleWidth;
    }
}
