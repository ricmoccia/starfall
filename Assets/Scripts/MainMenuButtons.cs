using UnityEngine;

// Componente di scena (NON singleton): ricreato fresco a ogni load del MainMenu, quindi i bottoni
// possono collegarsi a questo via inspector senza rischiare riferimenti a un'istanza distrutta.
// Delega all'istanza persistente di DifficultyManager tramite DifficultyManager.Instance.
public class MainMenuButtons : MonoBehaviour
{
    public void PlayEasy()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.PlayEasy();
        }
    }

    public void PlayNormal()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.PlayNormal();
        }
    }

    public void PlayHard()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.PlayHard();
        }
    }

    public void PlayImpossible()
    {
        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.PlayImpossible();
        }
    }
}
