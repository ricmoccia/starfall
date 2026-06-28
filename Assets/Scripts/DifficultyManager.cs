using System.Collections.Generic;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard, Impossible }

    public struct DifficultyMultipliers
    {
        public float enemyFireRateMult;        // moltiplica un intervallo in secondi: >1 = nemici sparano MENO
        public float enemyProjectileSpeedMult;
        public float enemyHealthMult;
        public float playerHealthMult;         // HP del player: 1.0 ovunque, >1 solo in Impossible
    }

    static readonly Dictionary<Difficulty, DifficultyMultipliers> presets =
        new Dictionary<Difficulty, DifficultyMultipliers>
        {
            { Difficulty.Easy,   new DifficultyMultipliers { enemyFireRateMult = 1.25f, enemyProjectileSpeedMult = 0.9f, enemyHealthMult = 0.9f, playerHealthMult = 1.0f } },
            { Difficulty.Normal, new DifficultyMultipliers { enemyFireRateMult = 1.0f, enemyProjectileSpeedMult = 1.0f, enemyHealthMult = 1.0f, playerHealthMult = 1.0f } },
            { Difficulty.Hard,   new DifficultyMultipliers { enemyFireRateMult = 0.85f, enemyProjectileSpeedMult = 1.15f, enemyHealthMult = 1.3f, playerHealthMult = 1.0f } },
            { Difficulty.Impossible, new DifficultyMultipliers { enemyFireRateMult = 0.70f, enemyProjectileSpeedMult = 1.3f, enemyHealthMult = 1.8f, playerHealthMult = 1.5f } },
        };

    // Punto unico da cui una futura submission leaderboard sceglierà l'ID per difficoltà. Non rinominare.
    public Difficulty Current { get; private set; } = Difficulty.Normal;

    public DifficultyMultipliers CurrentMultipliers => presets[Current];

    // Accessor verso l'istanza persistente sopravvissuta. I componenti di scena (es. MainMenuButtons)
    // delegano qui, perché un riferimento diretto all'istanza di scena diventa invalido dopo un reload.
    public static DifficultyManager Instance { get; private set; }

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

    public void SetDifficulty(Difficulty difficulty)
    {
        Current = difficulty;
    }

    // Overload per gli OnClick dell'inspector, che non possono passare un enum.
    public void SetDifficulty(int difficultyIndex)
    {
        Current = (Difficulty)difficultyIndex;
    }

    // Convenience per i bottoni del MainMenu: imposta la difficoltà e avvia la partita.
    public void PlayEasy() { SetDifficultyAndPlay(Difficulty.Easy); }
    public void PlayNormal() { SetDifficultyAndPlay(Difficulty.Normal); }
    public void PlayHard() { SetDifficultyAndPlay(Difficulty.Hard); }
    public void PlayImpossible() { SetDifficultyAndPlay(Difficulty.Impossible); }

    public void SetDifficultyAndPlay(int difficultyIndex)
    {
        SetDifficultyAndPlay((Difficulty)difficultyIndex);
    }

    void SetDifficultyAndPlay(Difficulty difficulty)
    {
        SetDifficulty(difficulty);
        FindFirstObjectByType<LevelManager>().LoadGame();
    }
}
