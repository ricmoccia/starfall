using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] WaveConfigSO[] waveConfigs;
    [SerializeField] float timeBetweenWaves = 1f;
    [SerializeField] bool isLooping;
    WaveConfigSO currentWave;

    // Cicli completi della sequenza di ondate (usato dal trigger del boss nel finale Impossible).
    int cyclesCompleted;
    // Pausa cooperativa: durante il boss lo spawn si ferma senza riavviare la coroutine.
    bool paused;

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        do
        {
            foreach (WaveConfigSO wave in waveConfigs)
            {
                currentWave = wave;
                for (int i = 0; i < currentWave.GetEnemyCount(); i++)
                {
                    while (paused) yield return null; // non spawnare durante il boss

                    Instantiate(
                        currentWave.GetEnemyPrefab(i),
                        currentWave.GetStartingWaypoint().position,
                        Quaternion.identity,
                        transform);

                    yield return new WaitForSeconds(currentWave.GetRandomEnemySpawnTime());
                }
                yield return new WaitForSeconds(timeBetweenWaves);
            }
            cyclesCompleted++; // un giro completo di tutte le ondate
        } while (isLooping);
    }

    public WaveConfigSO GetCurrentWave()
    {
        return currentWave;
    }

    public int GetCyclesCompleted()
    {
        return cyclesCompleted;
    }

    public void SetPaused(bool value)
    {
        paused = value;
    }
}
