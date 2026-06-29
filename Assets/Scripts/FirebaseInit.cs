using UnityEngine;
using Firebase;

// Step 1 della migrazione auth UGS -> Firebase: SOLO inizializzazione.
// All'avvio verifica/ripara le dipendenze Firebase e logga il DependencyStatus.
// Niente login, niente UI, niente FirebaseAuth: serve solo a confermare che Firebase parte.
public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance { get; private set; }

    // Risultato del check dipendenze, esposto per gli step futuri della migrazione.
    public DependencyStatus Status { get; private set; } = DependencyStatus.UnavailableOther;
    public bool Ready => Status == DependencyStatus.Available;

    void Awake()
    {
        ManageSingleton();
    }

    void ManageSingleton()
    {
        if (Instance != null)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    async void Start()
    {
        // Il duplicato disattivato da ManageSingleton non arriva qui: check solo sull'istanza viva.
        if (Instance != this)
        {
            return;
        }

        try
        {
            Status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (Status == DependencyStatus.Available)
            {
                Debug.Log("FirebaseInit: Firebase Available (DependencyStatus.Available).");
            }
            else
            {
                Debug.LogError("FirebaseInit: dipendenze Firebase NON disponibili: " + Status);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("FirebaseInit: inizializzazione Firebase fallita: " + e);
        }
    }
}
