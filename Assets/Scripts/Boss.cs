using System.Collections;
using UnityEngine;

// Entità boss autosufficiente: NON usa Health (resta intatto per player/nemici).
// Prende danno dai proiettili del player riusando DamageDealer; morte dedicata
// (niente game over, niente drop power-up). Macchina a stati esplicita a due fasi.
public class Boss : MonoBehaviour
{
    enum Phase { Phase1, Phase2, Dying }

    [Header("Health")]
    [SerializeField] int maxHealth = 2000;
    [SerializeField][Range(0f, 1f)] float phase2HealthThreshold = 0.5f; // entra in fase 2 a questa frazione

    [Header("Movement")]
    [SerializeField] float moveSpeed = 2f;            // lento avanti/indietro orizzontale (fase 1)
    [SerializeField] float phase2MoveMultiplier = 1.6f;
    [SerializeField] float topViewport = 0.85f;       // banda alta dello schermo
    [SerializeField] float horizontalPadding = 1.5f;

    [Header("Fan Attack (Phase 1)")]
    [SerializeField] GameObject projectilePrefab;     // riusa Enemy Projectile (layer Enemy)
    [SerializeField] int fanCount = 5;
    [SerializeField] float fanSpreadDegrees = 60f;    // ampiezza totale del ventaglio
    [SerializeField] float fireInterval = 2f;
    [SerializeField] float projectileSpeed = 8f;
    [SerializeField] float projectileLifetime = 6f;

    [Header("Phase 2")]
    [SerializeField] int phase2FanCount = 8;
    [SerializeField] float phase2FanSpreadDegrees = 90f;
    [SerializeField] float phase2FireInterval = 1.2f;
    [SerializeField] float aimedInterval = 1.5f;          // colpo mirato verso il player
    [SerializeField] float aimedProjectileSpeed = 11f;

    [Header("Enrage (transizione fase 2)")]
    [SerializeField] Color enrageColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] float enrageFlashDuration = 0.4f;
    [SerializeField] float enrageScalePunch = 1.15f;
    [SerializeField] AudioClip enrageClip;               // opzionale, sul gruppo SFX
    [SerializeField][Range(0, 1)] float enrageVolume = 0.6f;

    [Header("Death")]
    [SerializeField] int scoreBonus = 2000;
    [SerializeField] AudioClip explosionClip;
    [SerializeField][Range(0, 1)] float explosionVolume = 0.8f;
    [SerializeField] ParticleSystem explosionParticles;

    [Header("Juice")]
    [SerializeField] Color flashColor = Color.white;
    [SerializeField] float flashDuration = 0.06f;
    [SerializeField] float bossDeathShakeMagnitude = 0.25f;
    [SerializeField] float bossDeathShakeDuration = 0.4f;
    [SerializeField] float bossExplosionScale = 2f;

    Phase phase = Phase.Phase1;
    int currentHealth;
    int moveDir = 1;
    float minX, maxX;
    Camera cam;
    BossHealthBar healthBar;
    ScoreKeeper scoreKeeper;
    SpriteRenderer spriteRenderer;
    Transform player;
    CameraShake cameraShake;
    Color phaseBaseColor = Color.white;  // bianco fase 1 / enrageColor fase 2 (per il ritorno dopo il flash)
    Coroutine flashRoutine;

    void Start()
    {
        currentHealth = maxHealth;
        cam = Camera.main;
        InitBounds();

        healthBar = FindFirstObjectByType<BossHealthBar>(FindObjectsInactive.Include);
        scoreKeeper = FindFirstObjectByType<ScoreKeeper>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) phaseBaseColor = spriteRenderer.color;
        cameraShake = cam != null ? cam.GetComponent<CameraShake>() : null;
        CachePlayer();

        if (healthBar != null)
        {
            healthBar.Show();
            healthBar.SetFraction(1f);
        }

        StartCoroutine(Phase1Fire());
    }

    void InitBounds()
    {
        // Limiti orizzontali dal viewport (stesso pattern di PlayerController/PowerUp).
        Vector3 left = cam.ViewportToWorldPoint(new Vector3(0f, topViewport, 0f));
        Vector3 right = cam.ViewportToWorldPoint(new Vector3(1f, topViewport, 0f));
        minX = left.x + horizontalPadding;
        maxX = right.x - horizontalPadding;

        // Aggancia il boss alla banda alta.
        Vector3 p = transform.position;
        p.y = left.y;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.z = 0f;
        transform.position = p;
    }

    void Update()
    {
        if (phase == Phase.Dying) return;

        float speed = moveSpeed * (phase == Phase.Phase2 ? phase2MoveMultiplier : 1f);
        Vector3 p = transform.position;
        p.x += moveDir * speed * Time.deltaTime;
        if (p.x >= maxX) { p.x = maxX; moveDir = -1; }
        else if (p.x <= minX) { p.x = minX; moveDir = 1; }
        transform.position = p;
    }

    // ----- Fasi di fuoco -----

    IEnumerator Phase1Fire()
    {
        while (phase == Phase.Phase1)
        {
            yield return new WaitForSeconds(fireInterval);
            if (phase == Phase.Phase1) FireFan(fanCount, fanSpreadDegrees, projectileSpeed);
        }
    }

    IEnumerator Phase2Fire()
    {
        while (phase == Phase.Phase2)
        {
            yield return new WaitForSeconds(phase2FireInterval);
            if (phase == Phase.Phase2) FireFan(phase2FanCount, phase2FanSpreadDegrees, projectileSpeed);
        }
    }

    IEnumerator Phase2Aimed()
    {
        while (phase == Phase.Phase2)
        {
            yield return new WaitForSeconds(aimedInterval);
            if (phase == Phase.Phase2) FireAimed();
        }
    }

    // Transizione a metà vita: "enrage" visivo, poi parte la fase 2.
    IEnumerator EnterPhase2()
    {
        phase = Phase.Phase2; // termina Phase1Fire al prossimo controllo

        if (enrageClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(enrageClip, enrageVolume);
        }

        // Flash colore + scatto di scala, poi resta tinto di enrageColor (fase 2 leggibile a schermo).
        Vector3 baseScale = transform.localScale;
        Color baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float t = 0f;
        while (t < enrageFlashDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / enrageFlashDuration);
            // pop di scala: su e giù
            float punch = 1f + (enrageScalePunch - 1f) * Mathf.Sin(k * Mathf.PI);
            transform.localScale = baseScale * punch;
            // lampeggio verso bianco a metà, poi verso enrageColor
            if (spriteRenderer != null)
            {
                Color flash = Color.Lerp(Color.white, enrageColor, k);
                spriteRenderer.color = Color.Lerp(baseColor, flash, Mathf.Sin(k * Mathf.PI));
            }
            yield return null;
        }
        transform.localScale = baseScale;
        phaseBaseColor = enrageColor; // d'ora in poi il flash torna all'arancione di fase 2
        if (spriteRenderer != null) spriteRenderer.color = enrageColor;

        StartCoroutine(Phase2Fire());
        StartCoroutine(Phase2Aimed());
    }

    void FireFan(int count, float spreadDegrees, float speed)
    {
        if (projectilePrefab == null) return;

        float start = -spreadDegrees * 0.5f;
        float step = count > 1 ? spreadDegrees / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = start + step * i;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.down;
            SpawnProjectile(dir, speed);
        }
    }

    void FireAimed()
    {
        if (projectilePrefab == null) return;
        if (player == null) CachePlayer();
        if (player == null) return; // player non presente (es. appena morto): salta

        Vector2 dir = ((Vector2)(player.position - transform.position)).normalized;
        SpawnProjectile(dir, aimedProjectileSpeed);
    }

    void SpawnProjectile(Vector2 dir, float speed)
    {
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = dir * speed;
        Destroy(proj, projectileLifetime);
    }

    void CachePlayer()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        player = pc != null ? pc.transform : null;
    }

    // Lampeggio bianco breve, poi torna al colore di fase (bianco fase 1 / arancione fase 2).
    void Flash()
    {
        if (spriteRenderer == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = phaseBaseColor;
        flashRoutine = null;
    }

    // Danno in arrivo dai proiettili del player (layer Player vs Enemy: solo quelli triggerano).
    void OnTriggerEnter2D(Collider2D other)
    {
        if (phase == Phase.Dying) return;

        DamageDealer dealer = other.GetComponent<DamageDealer>();
        if (dealer == null) return;

        Flash();
        currentHealth -= dealer.GetDamage();
        dealer.Hit();

        if (healthBar != null)
        {
            healthBar.SetFraction(Mathf.Clamp01((float)currentHealth / maxHealth));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (phase == Phase.Phase1 && currentHealth <= maxHealth * phase2HealthThreshold)
        {
            StartCoroutine(EnterPhase2());
        }
    }

    void Die()
    {
        if (phase == Phase.Dying) return;
        phase = Phase.Dying;
        StopAllCoroutines(); // ferma tutti i loop di sparo: niente proiettili dopo la morte

        if (explosionParticles != null)
        {
            ParticleSystem fx = Instantiate(explosionParticles, transform.position, Quaternion.identity);
            fx.transform.localScale *= bossExplosionScale; // esplosione più grossa solo per il boss
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        // Shake un po' più marcato per la morte del boss (evento culminante).
        if (cameraShake != null)
        {
            cameraShake.Play(bossDeathShakeMagnitude, bossDeathShakeDuration);
        }

        // Esplosione grossa sul canale SFX.
        if (AudioManager.Instance != null && explosionClip != null)
        {
            AudioManager.Instance.PlaySFX(explosionClip, explosionVolume);
        }

        if (scoreKeeper != null)
        {
            scoreKeeper.ModifyScore(scoreBonus);
        }

        if (healthBar != null)
        {
            healthBar.Hide();
        }

        // Messaggio a schermo che appare e sfuma (non blocca il gioco).
        BossDefeatMessage message = FindFirstObjectByType<BossDefeatMessage>(FindObjectsInactive.Include);
        if (message != null)
        {
            message.Show("BOSS DEFEATED +" + scoreBonus);
        }

        Debug.Log("Boss defeated");
        Destroy(gameObject);
    }
}
