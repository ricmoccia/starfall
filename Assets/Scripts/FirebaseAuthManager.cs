using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Unity.Services.Core;
using Unity.Services.Authentication;

// Step C1 della migrazione: l'IDENTITÀ passa a Firebase (email+password+username).
// UGS resta SOLO per le leaderboard: dopo un login Firebase facciamo un "ponte" che
// assicura una sessione UGS attiva e imposta il PlayerName UGS = username Firebase,
// così invio/lettura punteggi continuano a funzionare senza toccare LeaderboardManager.
//
// Vive ACCANTO a GameServicesManager (login UGS attuale intatto fino allo Step C2).
public class FirebaseAuthManager : MonoBehaviour
{
    public struct AuthOutcome
    {
        public bool Success;
        public string Error;
        public static AuthOutcome Ok => new AuthOutcome { Success = true, Error = "" };
        public static AuthOutcome Fail(string message) => new AuthOutcome { Success = false, Error = message };
    }

    public static FirebaseAuthManager Instance { get; private set; }

    public event Action OnAuthChanged;

    bool firebaseReady;

    public FirebaseUser CurrentUser => FirebaseAuth.DefaultInstance?.CurrentUser;
    public bool IsEmailLoggedIn => CurrentUser != null;
    public string CurrentUsername => CurrentUser != null ? CurrentUser.DisplayName : "";
    public bool IsEmailVerified => CurrentUser != null && CurrentUser.IsEmailVerified;

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

    // Niente auto-login/ponte all'avvio in C1: il ponte parte solo dopo un login esplicito,
    // così il flusso UGS attuale resta intoccato. Il ripristino di sessione sarà C2.

    async Task EnsureFirebaseReady()
    {
        if (firebaseReady)
        {
            return;
        }
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            throw new Exception("Firebase dependencies not available: " + status);
        }
        firebaseReady = true;
    }

    // Registra l'utente Firebase, imposta displayName = username, invia la verifica email,
    // poi ESCE subito (coerente con "registra -> poi accedi").
    public async Task<AuthOutcome> RegisterWithEmailAsync(string email, string password, string username)
    {
        try
        {
            await EnsureFirebaseReady();

            AuthResult result = await FirebaseAuth.DefaultInstance
                .CreateUserWithEmailAndPasswordAsync(email, password);

            await result.User.UpdateUserProfileAsync(new UserProfile { DisplayName = username });
            await result.User.SendEmailVerificationAsync();

            // Non entrare subito: l'utente deve accedere con le credenziali appena create.
            FirebaseAuth.DefaultInstance.SignOut();
            OnAuthChanged?.Invoke();

            Debug.Log($"FirebaseAuthManager: registrazione OK per {email} (username '{username}'), email di verifica inviata.");
            return AuthOutcome.Ok;
        }
        catch (Exception e)
        {
            return AuthOutcome.Fail(MapError(e));
        }
    }

    // Login Firebase + ponte UGS (sessione + PlayerName) per le leaderboard.
    public async Task<AuthOutcome> LoginWithEmailAsync(string email, string password)
    {
        try
        {
            await EnsureFirebaseReady();

            AuthResult result = await FirebaseAuth.DefaultInstance
                .SignInWithEmailAndPasswordAsync(email, password);

            string username = result.User.DisplayName;
            // C1 non blocca i non verificati: lo stato è esposto, la UI deciderà in C2.
            await EnsureUgsSessionWithName(username);

            OnAuthChanged?.Invoke();
            Debug.Log($"FirebaseAuthManager: login OK per {email} (username '{username}', verificato={result.User.IsEmailVerified}).");
            return AuthOutcome.Ok;
        }
        catch (Exception e)
        {
            return AuthOutcome.Fail(MapError(e));
        }
    }

    // Ripristino sessione all'avvio: se Firebase ricorda un utente, riattiva il ponte UGS
    // (sessione + PlayerName) e segnala il login, così rientri nel menu senza ri-loggare.
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            await EnsureFirebaseReady();
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                return false;
            }
            await EnsureUgsSessionWithName(user.DisplayName);
            OnAuthChanged?.Invoke();
            Debug.Log($"FirebaseAuthManager: sessione ripristinata (username '{user.DisplayName}').");
            return true;
        }
        catch (Exception e)
        {
            // Non bloccare il menu: in caso di errore si mostra il pannello di login.
            Debug.LogWarning("FirebaseAuthManager: ripristino sessione fallito: " + e.Message);
            return false;
        }
    }

    // Reinvia l'email di verifica all'utente loggato non verificato.
    public async Task<AuthOutcome> ResendVerificationAsync()
    {
        try
        {
            await EnsureFirebaseReady();
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                return AuthOutcome.Fail("You are not logged in.");
            }
            // Aggiorna lo stato: potrebbe aver verificato dal browser nel frattempo.
            await user.ReloadAsync();
            if (user.IsEmailVerified)
            {
                OnAuthChanged?.Invoke();
                return AuthOutcome.Fail("Your email is already verified.");
            }
            await user.SendEmailVerificationAsync();
            Debug.Log("FirebaseAuthManager: email di verifica reinviata.");
            return AuthOutcome.Ok;
        }
        catch (Exception e)
        {
            return AuthOutcome.Fail(MapError(e));
        }
    }

    // Invia l'email di reset password all'indirizzo indicato.
    public async Task<AuthOutcome> SendPasswordResetAsync(string email)
    {
        try
        {
            await EnsureFirebaseReady();
            await FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(email);
            Debug.Log($"FirebaseAuthManager: email di reset password inviata a {email}.");
            return AuthOutcome.Ok;
        }
        catch (Exception e)
        {
            return AuthOutcome.Fail(MapResetError(e));
        }
    }

    // Mapping dedicato al reset: NON deve riusare "Invalid email or password." del login.
    static string MapResetError(Exception e)
    {
        FirebaseException fe = ExtractFirebase(e);
        if (fe != null)
        {
            switch ((AuthError)fe.ErrorCode)
            {
                case AuthError.InvalidEmail:
                case AuthError.MissingEmail:
                    return "Please enter a valid email address.";
                case AuthError.UserNotFound:
                    return "No account found with that email.";
                case AuthError.NetworkRequestFailed:
                    return "Network error. Check your connection and try again.";
                case AuthError.TooManyRequests:
                    return "Too many attempts. Please try again later.";
            }
        }
        return MapError(e);
    }

    public void LogoutEmail()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        // Chiudi anche la sessione UGS del ponte: l'identità si azzera del tutto.
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(true);
        }
        OnAuthChanged?.Invoke();
        Debug.Log("FirebaseAuthManager: logout (Firebase + sessione UGS del ponte).");
    }

    // PONTE UGS: garantisce una sessione UGS attiva e PlayerName = username Firebase.
    // Riusa la stessa AuthenticationService che le leaderboard già usano.
    async Task EnsureUgsSessionWithName(string username)
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        if (!string.IsNullOrEmpty(username))
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
        }
        Debug.Log($"FirebaseAuthManager: ponte UGS pronto, PlayerName='{AuthenticationService.Instance.PlayerName}'.");
    }

    // Messaggi leggibili in inglese dalle eccezioni Firebase.
    static string MapError(Exception e)
    {
        FirebaseException fe = ExtractFirebase(e);
        if (fe != null)
        {
            switch ((AuthError)fe.ErrorCode)
            {
                case AuthError.EmailAlreadyInUse:
                case AuthError.AccountExistsWithDifferentCredentials:
                    return "This email is already in use.";
                case AuthError.WeakPassword:
                    return "Password is too weak (use at least 6 characters).";
                case AuthError.InvalidEmail:
                case AuthError.MissingEmail:
                    return "Please enter a valid email address.";
                case AuthError.WrongPassword:
                case AuthError.UserNotFound:
                case AuthError.InvalidCredential:
                case AuthError.MissingPassword:
                    return "Invalid email or password.";
                case AuthError.NetworkRequestFailed:
                    return "Network error. Check your connection and try again.";
                case AuthError.TooManyRequests:
                    return "Too many attempts. Please try again later.";
            }
            return string.IsNullOrEmpty(fe.Message) ? "Authentication failed. Please try again." : fe.Message;
        }
        string msg = e.Message;
        return string.IsNullOrEmpty(msg) ? "Operation failed. Please try again." : msg;
    }

    static FirebaseException ExtractFirebase(Exception e)
    {
        if (e is AggregateException agg)
        {
            e = agg.Flatten().InnerException ?? e;
        }
        return e as FirebaseException ?? e.GetBaseException() as FirebaseException;
    }
}
