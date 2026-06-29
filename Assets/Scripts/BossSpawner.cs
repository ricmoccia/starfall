using UnityEngine;
using UnityEngine.InputSystem;

// Regista la comparsa del boss. SOLO in Impossible il boss compare come evento
// culminante dopo le ondate normali; nelle altre difficoltà l'auto-trigger non parte.
// Mantiene il tasto debug B (spawn singolo, qualsiasi difficoltà) e aggiunge N
// (forza il trigger del finale subito, per i test).
public class BossSpawner : MonoBehaviour
{
    enum DirectorState { Idle, BossActive, Cooldown }

    [SerializeField] GameObject bossPrefab;
    [SerializeField] GameObject bossAlienPrefab;  // variante alieno (per ora solo evocabile via debug)
    [SerializeField] float spawnViewportY = 0.85f;

    [Header("Debug keys")]
    [SerializeField] Key debugSpawnKey = Key.B;        // spawn corazzata singola, ovunque
    [SerializeField] Key debugSpawnAlienKey = Key.M;   // spawn alieno singolo (test della variante)
    [SerializeField] Key debugFinaleKey = Key.N;       // forza il trigger del finale

    [Header("Impossible finale")]
    [SerializeField] int bossAfterCycles = 1;       // primo boss dopo N cicli completi di ondate
    [SerializeField] float bossReappearCooldown = 20f; // dopo la sconfitta, ondate per X s, poi ricompare

    GameObject currentBoss;
    EnemySpawner enemySpawner;
    PlayerController player;
    DirectorState state = DirectorState.Idle;
    bool finaleEngaged;   // true mentre lo spawner è in pausa per il boss del finale
    float cooldownTimer;

    void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (Pressed(debugSpawnKey)) SpawnBoss();
        if (Pressed(debugSpawnAlienKey)) SpawnBoss(bossAlienPrefab); // debug: solo evocazione, nessuna logica di difficoltà
        if (Pressed(debugFinaleKey)) TriggerFinale();

        UpdateDirector();
    }

    void UpdateDirector()
    {
        switch (state)
        {
            case DirectorState.Idle:
                // Trigger di produzione: in Hard (alieno) e Impossible (corazzata), dopo un ciclo di ondate.
                if (FinaleBossPrefab() != null && currentBoss == null && enemySpawner != null
                    && enemySpawner.GetCyclesCompleted() >= bossAfterCycles)
                {
                    TriggerFinale();
                }
                break;

            case DirectorState.BossActive:
                if (PlayerGone())
                {
                    CleanupBossOnPlayerDeath();
                }
                else if (currentBoss == null)
                {
                    OnBossDefeated();
                }
                break;

            case DirectorState.Cooldown:
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    TriggerFinale();
                }
                break;
        }
    }

    // Avvia il finale: ferma lo spawn dei nemici (duello pulito) e fa comparire il boss della difficoltà.
    void TriggerFinale()
    {
        if (currentBoss != null) return; // un boss alla volta
        GameObject prefab = FinaleBossPrefab();
        if (prefab == null) return;      // Easy/Normal (o difficoltà ignota): nessun finale
        if (enemySpawner != null) enemySpawner.SetPaused(true);
        finaleEngaged = true;
        SpawnBoss(prefab);
        state = DirectorState.BossActive;
    }

    void OnBossDefeated()
    {
        // Il messaggio "BOSS DEFEATED +N" lo mostra Boss.Die(). Qui: riprendi le ondate + cooldown.
        if (finaleEngaged && enemySpawner != null) enemySpawner.SetPaused(false);
        finaleEngaged = false;
        cooldownTimer = bossReappearCooldown;
        state = DirectorState.Cooldown;
    }

    // Morte del player durante il boss: rimuovi boss + barra subito, niente stato sospeso.
    // Il flusso di Game Over normale (Health.Die) gira indipendentemente.
    void CleanupBossOnPlayerDeath()
    {
        if (currentBoss != null) Destroy(currentBoss);
        BossHealthBar bar = FindFirstObjectByType<BossHealthBar>(FindObjectsInactive.Include);
        if (bar != null) bar.Hide();
        finaleEngaged = false;
        state = DirectorState.Idle; // la scena passerà comunque a GameOver
    }

    // Spawn di produzione/finale: usa sempre la corazzata (bossPrefab). La scelta del boss
    // per difficoltà (alieno in Hard) è una tappa successiva.
    public void SpawnBoss() => SpawnBoss(bossPrefab);

    public void SpawnBoss(GameObject prefab)
    {
        if (prefab == null || currentBoss != null) return;

        Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, spawnViewportY, 0f));
        pos.z = 0f;
        currentBoss = Instantiate(prefab, pos, Quaternion.identity);
    }

    // Quale boss usa il finale per la difficoltà corrente. null = niente finale (Easy/Normal o ignota).
    // SOLO questa scelta cambia per difficoltà: comportamento/fasi/morte del boss restano identici.
    GameObject FinaleBossPrefab()
    {
        if (DifficultyManager.Instance == null) return null;
        switch (DifficultyManager.Instance.Current)
        {
            case DifficultyManager.Difficulty.Impossible: return bossPrefab;      // corazzata
            case DifficultyManager.Difficulty.Hard: return bossAlienPrefab;       // alieno
            default: return null;                                                 // Easy/Normal: nessun boss
        }
    }

    bool PlayerGone()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        return player == null;
    }

    static bool Pressed(Key key)
    {
        return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }
}
