using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class GameServicesManager : MonoBehaviour
{
    public enum AuthState { Initializing, SignedIn, NeedsLogin }

    public struct AuthResult
    {
        public bool Success;
        public string Error;

        public static AuthResult Ok => new AuthResult { Success = true, Error = "" };
        public static AuthResult Fail(string message) => new AuthResult { Success = false, Error = message };
    }

    public static GameServicesManager Instance { get; private set; }

    public AuthState State { get; private set; } = AuthState.Initializing;
    public event Action<AuthState> OnStateChanged;

    public bool IsSignedIn =>
        AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

    public string PlayerName => IsSignedIn ? AuthenticationService.Instance.PlayerName : "";

    // Set true after a successful registration so the UI can show the "account
    // created, please log in" state. Consume-once so it can't go stale.
    bool justRegistered;
    public bool ConsumeJustRegistered()
    {
        bool v = justRegistered;
        justRegistered = false;
        return v;
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

    async void Start()
    {
        // Il duplicato distrutto da ManageSingleton non arriva qui (è disattivato): init solo sull'istanza viva.
        if (Instance != this)
        {
            return;
        }

        try
        {
            await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                // Auto-login dalla sessione in cache (vale per qualsiasi provider).
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await EnsurePlayerName();
                SetState(AuthState.SignedIn);
            }
            else
            {
                SetState(AuthState.NeedsLogin);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("GameServicesManager init failed: " + e);
            SetState(AuthState.NeedsLogin);
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            // Imposta il nome mentre si è ancora loggati subito dopo il signup.
            await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
            // Esci subito: l'utente deve accedere con le credenziali appena create.
            AuthenticationService.Instance.SignOut();
            justRegistered = true;
            SetState(AuthState.NeedsLogin);
            return AuthResult.Ok;
        }
        catch (AuthenticationException e)
        {
            return AuthResult.Fail(MapError(e));
        }
        catch (RequestFailedException e)
        {
            return AuthResult.Fail(MapError(e));
        }
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            await EnsurePlayerName();
            SetState(AuthState.SignedIn);
            return AuthResult.Ok;
        }
        catch (AuthenticationException e)
        {
            return AuthResult.Fail(MapError(e));
        }
        catch (RequestFailedException e)
        {
            return AuthResult.Fail(MapError(e));
        }
    }

    public void SignOut()
    {
        // clearCredentials = true: cancella il token in cache → niente auto-login dello stesso account.
        AuthenticationService.Instance.SignOut(true);
        SetState(AuthState.NeedsLogin);
    }

    // Dopo login/auto-login il PlayerName locale è nullo finché non lo si recupera.
    // Best-effort: un errore qui NON deve far fallire il sign-in già riuscito.
    async Task EnsurePlayerName()
    {
        if (!string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName))
        {
            return;
        }
        try
        {
            await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameServicesManager: GetPlayerName failed: " + e.Message);
        }
    }

    static string MapError(Exception e)
    {
        string msg = e.Message ?? "";
        // Username già esistente (signup): nessun codice dedicato nell'SDK, si rileva dal messaggio.
        if (msg.IndexOf("exist", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "This username is already taken. Please choose another one.";
        }
        // Credenziali errate (login): messaggio del server o InvalidParameters.
        if (msg.IndexOf("WRONG_USERNAME_PASSWORD", StringComparison.OrdinalIgnoreCase) >= 0
            || msg.IndexOf("invalid username or password", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Invalid username or password.";
        }
        if (e is AuthenticationException ae && ae.ErrorCode == AuthenticationErrorCodes.InvalidParameters)
        {
            return "Invalid username or password.";
        }
        if (e is RequestFailedException rfe && rfe.ErrorCode == CommonErrorCodes.TransportError)
        {
            return "Network error. Check your connection and try again.";
        }
        return string.IsNullOrEmpty(msg) ? "Operation failed. Please try again." : msg;
    }

    void SetState(AuthState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}
