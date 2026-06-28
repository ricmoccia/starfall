using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AuthMenuUI : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] Button loginButton;
    [SerializeField] Button registerButton;
    [SerializeField] TMP_Text statusText;
    [SerializeField] GameObject difficultyGroup;
    [SerializeField] GameObject loggedInRow;
    [SerializeField] TMP_Text playerNameText;

    static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9.\-@_]{3,20}$");

    void Start()
    {
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        var gsm = GameServicesManager.Instance;
        if (gsm != null)
        {
            gsm.OnStateChanged += ApplyState;
            ApplyState(gsm.State); // gestisce il caso già SignedIn (ritorno al menu)
        }
    }

    void OnDestroy()
    {
        if (GameServicesManager.Instance != null)
        {
            GameServicesManager.Instance.OnStateChanged -= ApplyState;
        }
    }

    void ApplyState(GameServicesManager.AuthState state)
    {
        switch (state)
        {
            case GameServicesManager.AuthState.Initializing:
                loginPanel.SetActive(true);
                difficultyGroup.SetActive(false);
                if (loggedInRow != null) loggedInRow.SetActive(false);
                SetBusy(true);
                statusText.text = "Connecting…";
                break;
            case GameServicesManager.AuthState.NeedsLogin:
                loginPanel.SetActive(true);
                difficultyGroup.SetActive(false);
                if (loggedInRow != null) loggedInRow.SetActive(false);
                bool justRegistered = GameServicesManager.Instance != null
                                      && GameServicesManager.Instance.ConsumeJustRegistered();
                if (justRegistered)
                {
                    // Appena registrato: tieni lo username già digitato, svuota solo la password.
                    passwordInput.text = "";
                    statusText.text = "Account created - please log in";
                }
                else
                {
                    // Svuota i campi così si può inserire un account diverso.
                    usernameInput.text = "";
                    passwordInput.text = "";
                    statusText.text = "";
                }
                SetBusy(false);
                break;
            case GameServicesManager.AuthState.SignedIn:
                loginPanel.SetActive(false);
                difficultyGroup.SetActive(true);
                // Login riuscito: svuota i campi così il pannello è pulito se riappare (es. dopo logout).
                usernameInput.text = "";
                passwordInput.text = "";
                if (loggedInRow != null)
                {
                    loggedInRow.SetActive(true);
                    if (playerNameText != null)
                    {
                        playerNameText.text = "Logged in as " + StripSuffix(GameServicesManager.Instance.PlayerName);
                    }
                }
                break;
        }
    }

    public void OnLogoutClicked()
    {
        if (GameServicesManager.Instance != null)
        {
            GameServicesManager.Instance.SignOut();
        }
    }

    static string StripSuffix(string n)
    {
        return string.IsNullOrEmpty(n) ? "" : n.Split('#')[0];
    }

    public async void OnLoginClicked()
    {
        if (!ValidateInputs(out string error))
        {
            statusText.text = error;
            return;
        }

        SetBusy(true);
        statusText.text = "Please wait…";
        var result = await GameServicesManager.Instance.LoginAsync(usernameInput.text, passwordInput.text);
        if (!result.Success)
        {
            statusText.text = result.Error;
            SetBusy(false);
        }
        // Il successo è gestito da OnStateChanged -> SignedIn.
    }

    public async void OnRegisterClicked()
    {
        if (!ValidateInputs(out string error))
        {
            statusText.text = error;
            return;
        }

        SetBusy(true);
        statusText.text = "Please wait…";
        var result = await GameServicesManager.Instance.RegisterAsync(usernameInput.text, passwordInput.text);
        if (!result.Success)
        {
            statusText.text = result.Error;
            SetBusy(false);
        }
    }

    void SetBusy(bool busy)
    {
        loginButton.interactable = !busy;
        registerButton.interactable = !busy;
    }

    bool ValidateInputs(out string error)
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (!UsernameRegex.IsMatch(username))
        {
            error = "Username: 3-20 chars (letters, numbers, . - @ _).";
            return false;
        }
        if (!IsValidPassword(password))
        {
            error = "Password: 8-30 chars with lowercase, uppercase, number and symbol.";
            return false;
        }
        error = "";
        return true;
    }

    public static bool IsValidPassword(string password)
    {
        if (password == null || password.Length < 8 || password.Length > 30)
        {
            return false;
        }
        bool lower = false, upper = false, digit = false, symbol = false;
        foreach (char ch in password)
        {
            if (char.IsLower(ch)) lower = true;
            else if (char.IsUpper(ch)) upper = true;
            else if (char.IsDigit(ch)) digit = true;
            else symbol = true;
        }
        return lower && upper && digit && symbol;
    }

    public static bool IsValidUsername(string username)
    {
        return username != null && UsernameRegex.IsMatch(username);
    }
}
