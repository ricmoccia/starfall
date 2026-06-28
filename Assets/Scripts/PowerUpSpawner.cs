using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] GameObject firepowerPrefab;
    [SerializeField] GameObject lifePrefab;

    [Header("Drop chance per difficoltà")]
    [SerializeField][Range(0, 1)] float easyDropChance = 0.20f;
    [SerializeField][Range(0, 1)] float normalDropChance = 0.12f;
    [SerializeField][Range(0, 1)] float hardDropChance = 0.08f;
    [SerializeField][Range(0, 1)] float impossibleDropChance = 0.06f;
    [SerializeField][Range(0, 1)] float fallbackDropChance = 0.12f;

    public void TryDropAt(Vector3 pos)
    {
        float chance = GetDropChance();

        if (Random.value > chance)
        {
            return;
        }

        GameObject prefab = Random.value < 0.5f ? firepowerPrefab : lifePrefab;
        if (prefab != null)
        {
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    float GetDropChance()
    {
        if (DifficultyManager.Instance == null)
        {
            return fallbackDropChance;
        }

        switch (DifficultyManager.Instance.Current)
        {
            case DifficultyManager.Difficulty.Easy: return easyDropChance;
            case DifficultyManager.Difficulty.Hard: return hardDropChance;
            case DifficultyManager.Difficulty.Impossible: return impossibleDropChance;
            default: return normalDropChance;
        }
    }
}
