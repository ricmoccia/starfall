using UnityEngine;
using Unity.Services.Authentication;

// Dopo lo Step C2 l'identità utente è di Firebase (FirebaseAuthManager). UGS resta SOLO come
// ponte per le leaderboard: la sessione UGS è gestita dal ponte in FirebaseAuthManager.
// Questo manager è ridotto a un sottile accessore di sola lettura sulla sessione UGS, usato da
// LeaderboardManager (IsSignedIn) — che resta invariato.
public class GameServicesManager : MonoBehaviour
{
    public static GameServicesManager Instance { get; private set; }

    // Difensivo: AuthenticationService può non essere ancora inizializzato (il ponte lo inizializza
    // al primo login/restore). In quel caso "non loggato".
    public bool IsSignedIn
    {
        get
        {
            try { return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn; }
            catch { return false; }
        }
    }

    public string PlayerName
    {
        get
        {
            try { return IsSignedIn ? AuthenticationService.Instance.PlayerName : ""; }
            catch { return ""; }
        }
    }

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
}
