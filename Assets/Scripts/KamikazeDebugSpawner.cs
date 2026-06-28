using UnityEngine;
using UnityEngine.InputSystem;

// Strumento di debug: evoca un kamikaze in cima allo schermo (tasto K) per i test in editor.
public class KamikazeDebugSpawner : MonoBehaviour
{
    [SerializeField] GameObject kamikazePrefab;
    [SerializeField] Key debugKey = Key.K;
    [SerializeField] float spawnViewportY = 0.95f;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[debugKey].wasPressedThisFrame)
        {
            SpawnKamikaze();
        }
    }

    public void SpawnKamikaze()
    {
        if (kamikazePrefab == null) return;

        Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, spawnViewportY, 0f));
        pos.z = 0f;
        Instantiate(kamikazePrefab, pos, Quaternion.identity);
    }
}
