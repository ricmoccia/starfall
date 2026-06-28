using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] bool isPlayer;
    [SerializeField] int scoreValue = 50;
    [SerializeField] int health = 50;
    [SerializeField] ParticleSystem hitParticles;

    [Header("Enemy death shake (solo nemici)")]
    [SerializeField] float enemyDeathShakeMagnitude = 0.06f;
    [SerializeField] float enemyDeathShakeDuration = 0.08f;

    [Header("Hit flash — nemici")]
    [SerializeField] Color flashColor = Color.white;
    [SerializeField] float flashDuration = 0.06f;

    [Header("Hit flash — player")]
    [SerializeField] Color playerFlashColor = new Color(1f, 0.25f, 0.25f); // rosso leggibile
    [SerializeField] float playerFlashDuration = 0.10f;

    CameraShake cameraShake;
    AudioManager audioManager;
    ScoreKeeper scoreKeeper;
    LevelManager levelManager;

    SpriteRenderer spriteRenderer;
    Color baseColor = Color.white;
    Coroutine flashRoutine;

    int maxHealth;

    void Awake()
    {
        ApplyDifficulty();

        // Salvato dopo l'eventuale scaling difficoltà: per il player è l'HP iniziale, cap della cura.
        maxHealth = health;
    }

    void ApplyDifficulty()
    {
        DifficultyManager difficultyManager = FindFirstObjectByType<DifficultyManager>();

        // Fallback ×1.0 se non c'è un DifficultyManager in scena (es. GameScene giocata da sola).
        if (difficultyManager == null)
        {
            return;
        }

        // Player scalato da playerHealthMult (>1 solo in Impossible), nemici da enemyHealthMult.
        float mult = isPlayer
            ? difficultyManager.CurrentMultipliers.playerHealthMult
            : difficultyManager.CurrentMultipliers.enemyHealthMult;

        health = Mathf.RoundToInt(health * mult);
    }

    void Start()
    {
        cameraShake = Camera.main.GetComponent<CameraShake>();
        audioManager = FindFirstObjectByType<AudioManager>();
        scoreKeeper = FindFirstObjectByType<ScoreKeeper>();
        levelManager = FindFirstObjectByType<LevelManager>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        DamageDealer damageDealer = other.GetComponent<DamageDealer>();

        if (damageDealer != null)
        {
            // Flash prima del danno: anche il colpo letale mostra il lampeggio (l'oggetto sparisce subito dopo).
            Flash();

            TakeDamage(damageDealer.GetDamage());
            PlayHitParticles();
            damageDealer.Hit();
            audioManager.PlayDamageSFX();
        }
    }

    // Lampeggio breve quando si subisce danno: rosso per il player, bianco per i nemici. Poi torna al colore base.
    void Flash()
    {
        if (spriteRenderer == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);

        Color color = isPlayer ? playerFlashColor : flashColor;
        float duration = isPlayer ? playerFlashDuration : flashDuration;
        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = baseColor;
        flashRoutine = null;
    }

    void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isPlayer)
        {
            // Sting di morte sul manager persistente: suona anche durante il Destroy + la transizione a GameOver.
            audioManager.PlayPlayerDeath();
            // Legge il VALORE finale prima del Destroy/reset e lo invia sul manager persistente.
            int finalScore = scoreKeeper.GetScore();
            LeaderboardManager.Instance?.SubmitScore(finalScore);
            levelManager.LoadGameOver();
        }
        else
        {
            audioManager.PlayEnemyExplosion();
            // Shake appena percettibile sull'esplosione nemico.
            if (cameraShake != null)
            {
                cameraShake.Play(enemyDeathShakeMagnitude, enemyDeathShakeDuration);
            }
            scoreKeeper.ModifyScore(scoreValue);
            // Drop opzionale: nessuno spawner in scena → nessun drop (feature disattivabile).
            FindFirstObjectByType<PowerUpSpawner>()?.TryDropAt(transform.position);
        }
        Destroy(gameObject);
    }

    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    void PlayHitParticles()
    {
        if (hitParticles != null)
        {
            ParticleSystem particles = Instantiate(hitParticles, transform.position, Quaternion.identity);
            Destroy(particles, particles.main.duration + particles.main.startLifetime.constantMax);
        }
    }

    public int GetHealth()
    {
        return health;
    }
}
