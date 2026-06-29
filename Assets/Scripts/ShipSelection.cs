using UnityEngine;

// Sorgente di verità della nave scelta: indice persistito in PlayerPrefs.
// Helper statico (niente oggetto di scena), così funziona anche avviando GameScene direttamente.
public static class ShipSelection
{
    const string Key = "SelectedShipIndex";

    // Indice salvato, ripiegato dentro [0, count) per non uscire mai dal catalogo.
    public static int GetIndex(int count)
    {
        if (count <= 0) return 0;
        int i = PlayerPrefs.GetInt(Key, 0);
        return Mathf.Clamp(i, 0, count - 1);
    }

    public static void SetIndex(int index)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }
}
