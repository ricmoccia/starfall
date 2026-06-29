using UnityEngine;

// Catalogo delle navi selezionabili (solo aspetto). Ogni voce = sprite + nome + scala visiva.
// Stesso pattern di WaveConfigSO: ScriptableObject con [CreateAssetMenu] e getter incapsulati.
// Indice 0 = nave attuale (blu) = default, così senza scelta il comportamento resta invariato.
[CreateAssetMenu(fileName = "ShipLibrary", menuName = "New ShipLibrary")]
public class ShipLibrarySO : ScriptableObject
{
    [System.Serializable]
    public struct ShipEntry
    {
        public string displayName;
        public Sprite sprite;
        // Le navi nuove importano a PPU diverso: la scala normalizza la dimensione a schermo
        // (solo estetica, il collider del player NON cambia).
        public float visualScale;
    }

    [SerializeField] ShipEntry[] ships;

    public int Count => ships != null ? ships.Length : 0;

    int Clamp(int index) => Count == 0 ? 0 : Mathf.Clamp(index, 0, Count - 1);

    public Sprite GetSprite(int index) => Count == 0 ? null : ships[Clamp(index)].sprite;

    public string GetName(int index) => Count == 0 ? "" : ships[Clamp(index)].displayName;

    // Scala visiva della voce; 0/non impostata viene trattata come 1 per sicurezza.
    public float GetScale(int index)
    {
        if (Count == 0) return 1f;
        float s = ships[Clamp(index)].visualScale;
        return s <= 0f ? 1f : s;
    }
}
