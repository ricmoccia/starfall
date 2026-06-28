using System.Collections;
using UnityEngine;

public class PlayerBuffs : MonoBehaviour
{
    [SerializeField] float firepowerDuration = 7f;
    [SerializeField] float firepowerMultiplier = 0.5f;
    [SerializeField] int healAmount = 20;

    Shooter playerShooter;
    Health playerHealth;

    bool firepowerActive;
    float originalFireRate;
    float originalMinFireRate;
    Coroutine firepowerRoutine;

    void Awake()
    {
        playerShooter = GetComponent<Shooter>();
        playerHealth = GetComponent<Health>();
    }

    public void Apply(PowerUp.PowerUpType type)
    {
        switch (type)
        {
            case PowerUp.PowerUpType.Firepower:
                ApplyFirepower();
                break;
            case PowerUp.PowerUpType.Health:
                playerHealth.Heal(healAmount);
                break;
        }
    }

    void ApplyFirepower()
    {
        // Solo alla prima attivazione: salva gli originali e dimezza. Se già attivo, non risalvare
        // e non ridimezzare (eviterebbe di accumulare il moltiplicatore) — si rinfresca solo il timer.
        if (!firepowerActive)
        {
            originalFireRate = playerShooter.GetBaseFireRate();
            originalMinFireRate = playerShooter.GetMinimumFireRate();
            playerShooter.SetBaseFireRate(originalFireRate * firepowerMultiplier);
            playerShooter.SetMinimumFireRate(originalMinFireRate * firepowerMultiplier);
            firepowerActive = true;
        }

        if (firepowerRoutine != null)
        {
            StopCoroutine(firepowerRoutine);
        }
        firepowerRoutine = StartCoroutine(FirepowerTimer());
    }

    IEnumerator FirepowerTimer()
    {
        yield return new WaitForSeconds(firepowerDuration);

        playerShooter.SetBaseFireRate(originalFireRate);
        playerShooter.SetMinimumFireRate(originalMinFireRate);
        firepowerActive = false;
        firepowerRoutine = null;
    }
}
