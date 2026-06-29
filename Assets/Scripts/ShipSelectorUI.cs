using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Riga "SELECT SHIP" nel menu: anteprima della nave corrente + frecce sinistra/destra.
// Si auto-gestisce la visibilità (solo da loggati, come i bottoni di difficoltà) ascoltando
// FirebaseAuthManager.OnAuthChanged, così NON serve toccare AuthMenuUI/Firebase.
public class ShipSelectorUI : MonoBehaviour
{
    [SerializeField] ShipLibrarySO library;
    [SerializeField] Image preview;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;

    int index;

    void Start()
    {
        if (leftButton != null) leftButton.onClick.AddListener(Prev);
        if (rightButton != null) rightButton.onClick.AddListener(Next);

        index = library != null ? ShipSelection.GetIndex(library.Count) : 0;
        Render();

        // Subscribe in Start (persistente): il delegate viene invocato anche se la riga è disattivata,
        // quindi al login la riga può tornare visibile.
        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance.OnAuthChanged += RefreshVisibility;
        RefreshVisibility();
    }

    void OnDestroy()
    {
        if (FirebaseAuthManager.Instance != null)
            FirebaseAuthManager.Instance.OnAuthChanged -= RefreshVisibility;
    }

    void Prev() { Step(-1); }
    void Next() { Step(1); }

    void Step(int dir)
    {
        if (library == null || library.Count == 0) return;
        index = (index + dir + library.Count) % library.Count; // wrap circolare
        ShipSelection.SetIndex(index);
        Render();
    }

    void Render()
    {
        if (library == null || library.Count == 0) return;
        if (preview != null)
        {
            preview.sprite = library.GetSprite(index);
            preview.preserveAspect = true;
            preview.enabled = preview.sprite != null;
        }
        if (nameLabel != null) nameLabel.text = library.GetName(index);
    }

    void RefreshVisibility()
    {
        bool loggedIn = FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsEmailLoggedIn;
        if (gameObject.activeSelf != loggedIn) gameObject.SetActive(loggedIn);
        if (loggedIn) Render();
    }
}
