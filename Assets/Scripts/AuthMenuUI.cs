using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Step C2: la UI di autenticazione passa a Firebase (email+password+username).
// UGS resta solo come ponte leaderboard (in FirebaseAuthManager). Un solo pannello con due modalità:
// LOG IN (email+password) <-> SIGN UP (email+password+username), via il bottone "toggle".
public class AuthMenuUI : MonoBehaviour
{
    enum Mode { Login, SignUp }

    [SerializeField] GameObject loginPanel;
    [SerializeField] TMP_InputField emailInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] TMP_InputField usernameInput;   // mostrato solo in SIGN UP
    [SerializeField] Button primaryButton;           // LOG IN / SIGN UP
    [SerializeField] Button toggleButton;            // Create account / Back to login
    [SerializeField] TMP_Text primaryLabel;
    [SerializeField] TMP_Text toggleLabel;
    [SerializeField] TMP_Text statusText;
    [SerializeField] GameObject difficultyGroup;
    [SerializeField] GameObject loggedInRow;
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] Button resendButton;   // RESEND VERIFICATION EMAIL (loggato + non verificato)
    [SerializeField] Button forgotButton;    // FORGOT PASSWORD? (pannello login)

    static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9.\-@_]{3,20}$");
    static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    Mode mode = Mode.Login;
    CanvasGroup loginPanelCg;

    async void Start()
    {
        emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
        emailInput.ForceLabelUpdate();
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        // Il login panel si nasconde via CanvasGroup (resta SEMPRE attivo): così un campo focalizzato
        // non viene mai disattivato a metà evento, evitando il blocco dell'input al ri-login dopo logout.
        loginPanel.SetActive(true);
        loginPanelCg = loginPanel.GetComponent<CanvasGroup>();
        if (loginPanelCg == null) loginPanelCg = loginPanel.AddComponent<CanvasGroup>();

        var auth = FirebaseAuthManager.Instance;
        if (auth != null) auth.OnAuthChanged += RefreshAuthUI;

        SetMode(Mode.Login);

        // Auto-login: se Firebase ricorda la sessione, rientra diretto nel menu (ponte UGS riattivato).
        if (auth != null) await auth.TryRestoreSessionAsync();
        RefreshAuthUI();
    }

    void OnDestroy()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnAuthChanged -= RefreshAuthUI;
        }
    }

    void RefreshAuthUI()
    {
        // PRIMA di cambiare i pannelli: togli il focus, così nessun InputField resta "selected"
        // mentre il pannello cambia (causa del blocco input al ri-login dopo logout).
        ClearFocus();

        var auth = FirebaseAuthManager.Instance;
        bool loggedIn = auth != null && auth.IsEmailLoggedIn;

        SetLoginPanelVisible(!loggedIn);
        difficultyGroup.SetActive(loggedIn);
        if (loggedInRow != null) loggedInRow.SetActive(loggedIn);

        if (loggedIn)
        {
            // Avviso non bloccante (per ora non impedisce il login).
            SetVerifyLine(auth.IsEmailVerified ? null : "Your email is not verified yet");
            if (resendButton != null) resendButton.gameObject.SetActive(!auth.IsEmailVerified);
        }
        else
        {
            ApplyMode(); // pannello pulito nello stato corrente
        }
    }

    // Toglie il focus: deseleziona dall'EventSystem, chiude l'editing dei campi e, su mobile,
    // CHIUDE esplicitamente la TouchScreenKeyboard (i campi hanno hideSoftKeyboard=false, quindi
    // DeactivateInputField NON la chiude da solo: resterebbe agganciata e bloccherebbe i tocchi al ri-login).
    void ClearFocus()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (emailInput != null) emailInput.DeactivateInputField();
        if (passwordInput != null) passwordInput.DeactivateInputField();
        if (usernameInput != null) usernameInput.DeactivateInputField();

        // Dopo aver deselezionato (il riferimento alla keyboard sopravvive a DeactivateInputField con
        // hideSoftKeyboard=false), forza la chiusura della tastiera software.
        CloseSoftKeyboard();
        // Nessun auto-focus al ritorno: la selezione è azzerata sopra e nessuno seleziona/attiva un campo
        // alla ricomparsa del pannello, quindi shouldActivateOnSelect non riapre la tastiera (solo un tap reale).
    }

    // Chiude la tastiera software di sistema (Android/iOS) per ciascun campo + fallback globale.
    // Logga lo stato per il test su device (adb logcat -s Unity), così si vede se il fix agisce.
    void CloseSoftKeyboard()
    {
        string ctx = (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsEmailLoggedIn)
            ? "login-success" : "logout/refresh";
        bool visibleBefore = TouchScreenKeyboard.visible;

        string e = CloseFieldKeyboard(emailInput);
        string p = CloseFieldKeyboard(passwordInput);
        string u = CloseFieldKeyboard(usernameInput);

        Debug.Log($"[AuthKB] {ctx}: email({e}) password({p}) username({u}) supported={TouchScreenKeyboard.isSupported} visibleBefore={visibleBefore} visibleAfter={TouchScreenKeyboard.visible}");

        // Fallback globale: se una tastiera è ancora visibile (riferimento di campo perso), forzane il rilascio.
        if (TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible)
        {
            TouchScreenKeyboard g = TouchScreenKeyboard.Open("");
            if (g != null) g.active = false;
            Debug.Log($"[AuthKB] {ctx}: ancora visibile → fallback globale Open(\"\").active=false → visibleNow={TouchScreenKeyboard.visible}");
        }
    }

    // Chiude la keyboard del singolo campo se presente; ritorna una stringa di stato per il log.
    string CloseFieldKeyboard(TMP_InputField field)
    {
        if (field == null) return "null-field";
        TouchScreenKeyboard k = field.touchScreenKeyboard;
        if (k == null) return "kb=false";
        TouchScreenKeyboard.Status status = k.status;
        k.active = false;
        return $"kb=true,statusPrima={status}";
    }

    // Mostra/nasconde il login panel SENZA disattivarlo: da nascosto non intercetta i tocchi
    // (blocksRaycasts=false, come il fix del banner BOSS DEFEATED); da mostrato torna pienamente interattivo.
    void SetLoginPanelVisible(bool visible)
    {
        if (loginPanelCg == null) { loginPanel.SetActive(visible); return; }
        loginPanelCg.alpha = visible ? 1f : 0f;
        loginPanelCg.blocksRaycasts = visible;
        loginPanelCg.interactable = visible;
    }

    // Riga sotto al nome: avviso "non verificato" o feedback del reinvio. Vuota = solo il nome.
    void SetVerifyLine(string line)
    {
        var auth = FirebaseAuthManager.Instance;
        if (playerNameText == null || auth == null) return;
        string text = "Logged in as " + auth.CurrentUsername;
        if (!string.IsNullOrEmpty(line)) text += "\n" + line;
        playerNameText.text = text;
    }

    void SetMode(Mode m)
    {
        mode = m;
        ApplyMode();
        statusText.text = "";
    }

    void ApplyMode()
    {
        bool signUp = mode == Mode.SignUp;
        usernameInput.gameObject.SetActive(signUp);
        if (primaryLabel != null) primaryLabel.text = signUp ? "SIGN UP" : "LOG IN";
        if (toggleLabel != null) toggleLabel.text = signUp ? "Back to login" : "Create account";

        // Layout ordinato in entrambe le modalità: lo username compare tra password e bottoni
        // (signup) e i bottoni/stato scendono di una riga; in login non c'è buco. Email(360) e
        // password(210) restano fissi.
        var user = (RectTransform)usernameInput.transform;
        var prim = (RectTransform)primaryButton.transform;
        var tog = (RectTransform)toggleButton.transform;
        var stat = (RectTransform)statusText.transform;
        // "Forgot password?" solo in LOGIN, discreto sotto i bottoni.
        if (forgotButton != null) forgotButton.gameObject.SetActive(!signUp);
        if (signUp)
        {
            user.anchoredPosition = new Vector2(0f, 40f);
            prim.anchoredPosition = new Vector2(0f, -130f);
            tog.anchoredPosition = new Vector2(0f, -300f);
            stat.anchoredPosition = new Vector2(0f, -440f);
        }
        else
        {
            prim.anchoredPosition = new Vector2(0f, 40f);
            tog.anchoredPosition = new Vector2(0f, -130f);
            if (forgotButton != null) ((RectTransform)forgotButton.transform).anchoredPosition = new Vector2(0f, -250f);
            stat.anchoredPosition = new Vector2(0f, -350f);
        }
    }

    // Reinvio email di verifica (vista loggata).
    public async void OnResendClicked()
    {
        var auth = FirebaseAuthManager.Instance;
        if (auth == null) return;
        if (resendButton != null) resendButton.interactable = false;
        var r = await auth.ResendVerificationAsync();
        if (resendButton != null) resendButton.interactable = true;
        if (auth.IsEmailVerified)
        {
            RefreshAuthUI(); // ora verificato: vista pulita, bottone nascosto
        }
        else
        {
            SetVerifyLine(r.Success ? "Verification email sent." : r.Error);
        }
    }

    // Reset password (pannello login): usa l'email già digitata.
    public async void OnForgotClicked()
    {
        if (!EmailRegex.IsMatch(emailInput.text ?? ""))
        {
            statusText.text = "Please enter your email above first.";
            return;
        }
        SetBusy(true);
        statusText.text = "Please wait…";
        var r = await FirebaseAuthManager.Instance.SendPasswordResetAsync(emailInput.text);
        SetBusy(false);
        statusText.text = r.Success ? "Password reset email sent - check your inbox." : r.Error;
    }

    public void OnToggleClicked()
    {
        SetMode(mode == Mode.Login ? Mode.SignUp : Mode.Login);
    }

    public void OnPrimaryClicked()
    {
        if (mode == Mode.Login) DoLogin();
        else DoRegister();
    }

    async void DoLogin()
    {
        if (!ValidateEmail(out string e1)) { statusText.text = e1; return; }
        if (!ValidatePassword(out string e2)) { statusText.text = e2; return; }

        SetBusy(true);
        statusText.text = "Please wait…";
        var result = await FirebaseAuthManager.Instance.LoginWithEmailAsync(emailInput.text, passwordInput.text);
        if (!result.Success)
        {
            statusText.text = result.Error;
            SetBusy(false);
        }
        // Successo: OnAuthChanged -> RefreshAuthUI entra nel menu.
    }

    async void DoRegister()
    {
        if (!ValidateEmail(out string e1)) { statusText.text = e1; return; }
        if (!ValidatePassword(out string e2)) { statusText.text = e2; return; }
        if (!ValidateUsername(out string e3)) { statusText.text = e3; return; }

        SetBusy(true);
        statusText.text = "Please wait…";
        var result = await FirebaseAuthManager.Instance.RegisterWithEmailAsync(
            emailInput.text, passwordInput.text, usernameInput.text);
        SetBusy(false);

        if (result.Success)
        {
            // Registra -> conferma -> torna al login (NON entrare subito).
            SetMode(Mode.Login);
            passwordInput.text = "";
            usernameInput.text = "";
            // Email lasciata pre-compilata.
            statusText.text = "Account created - please check your email to verify, then log in";
        }
        else
        {
            statusText.text = result.Error;
        }
    }

    public void OnLogoutClicked()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.LogoutEmail();
        }
        emailInput.text = "";
        passwordInput.text = "";
        usernameInput.text = "";
        SetMode(Mode.Login);
        // OnAuthChanged -> RefreshAuthUI mostra il pannello di login pulito.
    }

    void SetBusy(bool busy)
    {
        primaryButton.interactable = !busy;
        toggleButton.interactable = !busy;
        if (forgotButton != null) forgotButton.interactable = !busy;
    }

    bool ValidateEmail(out string error)
    {
        if (!EmailRegex.IsMatch(emailInput.text ?? ""))
        {
            error = "Please enter a valid email address.";
            return false;
        }
        error = "";
        return true;
    }

    bool ValidatePassword(out string error)
    {
        if (!IsValidPassword(passwordInput.text))
        {
            error = "Password: 8-30 chars with lowercase, uppercase, number and symbol.";
            return false;
        }
        error = "";
        return true;
    }

    bool ValidateUsername(out string error)
    {
        if (!UsernameRegex.IsMatch(usernameInput.text ?? ""))
        {
            error = "Username: 3-20 chars (letters, numbers, . - @ _).";
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
