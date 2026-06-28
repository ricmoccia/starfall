using System.Collections;
using UnityEngine;

public class Shooter : MonoBehaviour
{
    [Header("Base Variables")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] float projectileLifetime = 5f;
    [SerializeField] float baseFireRate = 0.2f;

    [Header("AI Variables")]
    [SerializeField] bool useAI;
    [SerializeField] float minimumFireRate = 0.2f;
    [SerializeField] float fireRateVariance = 0f;

    [HideInInspector] public bool isFiring;
    Coroutine fireCoroutine;
    AudioManager audioManager;

    void Awake()
    {
        if (useAI)
        {
            ApplyDifficulty();
        }
    }

    void ApplyDifficulty()
    {
        DifficultyManager difficultyManager = FindFirstObjectByType<DifficultyManager>();

        // Fallback ×1.0 se non c'è un DifficultyManager in scena (es. GameScene giocata da sola).
        if (difficultyManager == null)
        {
            return;
        }

        DifficultyManager.DifficultyMultipliers multipliers = difficultyManager.CurrentMultipliers;

        // Scala sia baseFireRate sia minimumFireRate: altrimenti il clamp su minimumFireRate
        // in FireContinuously annullerebbe l'effetto su Hard.
        baseFireRate *= multipliers.enemyFireRateMult;
        minimumFireRate *= multipliers.enemyFireRateMult;
        projectileSpeed *= multipliers.enemyProjectileSpeedMult;
    }

    void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();

        if (useAI)
        {
            isFiring = true;
        }
    }

    void Update()
    {
        Fire();
    }

    // Accessor per i buff a runtime (es. PlayerBuffs Firepower). baseFireRate e minimumFireRate
    // vanno scalati insieme: il clamp su minimumFireRate in FireContinuously annullerebbe altrimenti l'effetto.
    public float GetBaseFireRate() => baseFireRate;
    public void SetBaseFireRate(float value) => baseFireRate = value;
    public float GetMinimumFireRate() => minimumFireRate;
    public void SetMinimumFireRate(float value) => minimumFireRate = value;

    void Fire()
    {
        if (isFiring && fireCoroutine == null)
        {
            fireCoroutine = StartCoroutine(FireContinuously());
        }
        else if (!isFiring && fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    IEnumerator FireContinuously()
    {
        while (true)
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            projectile.transform.rotation = transform.rotation;

            Rigidbody2D projectileRB = projectile.GetComponent<Rigidbody2D>();
            projectileRB.linearVelocity = transform.up * projectileSpeed;

            Destroy(projectile, projectileLifetime);

            float waitTime = Random.Range(baseFireRate - fireRateVariance, baseFireRate + fireRateVariance);
            waitTime = Mathf.Clamp(waitTime, minimumFireRate, float.MaxValue);

            audioManager.PlayShootingSFX();

            yield return new WaitForSeconds(waitTime);
        }
    }
}
