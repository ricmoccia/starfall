using UnityEngine;

// Sul ROOT del prefab Player: applica SOLO lo sprite (e la scala visiva) della nave scelta
// allo SpriteRenderer figlio. Nessun effetto su movimento/sparo/collisioni/Health.
public class ShipSkinApplier : MonoBehaviour
{
    [SerializeField] ShipLibrarySO library;
    [SerializeField] SpriteRenderer spriteRenderer; // se vuoto: cercato tra i figli (come fa Health)

    void Start()
    {
        if (library == null || library.Count == 0) return;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        int index = ShipSelection.GetIndex(library.Count);

        Sprite sprite = library.GetSprite(index);
        if (sprite != null) spriteRenderer.sprite = sprite;

        // Scala estetica sul transform dello SpriteRenderer (il collider è sul root, resta intatto).
        float scale = library.GetScale(index);
        spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
