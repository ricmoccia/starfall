using System.Collections;
using UnityEngine;

// Nemico kamikaze: entra dall'alto, aggancia il player e si lancia contro (dive).
// NON spara. Riusa Health (morte/punteggio) e DamageDealer (danno da contatto).
[RequireComponent(typeof(Rigidbody2D))]
public class KamikazeDive : MonoBehaviour
{
    enum State { Entering, Telegraphing, Diving }

    [Header("Entry / acquisizione")]
    [SerializeField] float entrySpeed = 3f;          // discesa iniziale
    [SerializeField] float acquisitionRadius = 7f;   // distanza dal player che fa scattare il dive
    [SerializeField] float entryViewportY = 0.95f;   // entra sempre dall'alto, dentro lo schermo
    [SerializeField] float entryXPadding = 2.5f;     // margine dai bordi laterali: più alto = ingresso più centrale

    [Header("Telegraph")]
    [SerializeField] float telegraphDuration = 0.5f; // pausa + segnale prima del lancio
    [SerializeField] Color telegraphColor = new Color(1f, 0.2f, 0.2f); // rosso: visibile anche su sprite chiaro
    [SerializeField] float telegraphBlinkInterval = 0.08f;
    [SerializeField] float telegraphScalePunch = 1.25f; // pulsazione di scala (solo sprite child, hitbox invariata)

    [Header("Dive")]
    [SerializeField] float diveSpeed = 12f;          // lancio veloce (in linea retta, schivabile)
    [SerializeField] float offscreenMargin = 1.5f;   // distrutto sotto questo margine fuori schermo

    State state = State.Entering;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Color baseColor = Color.white;
    Transform player;
    float bottomBound;
    float topY;
    float minX, maxX;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
        CachePlayer();

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 left = cam.ViewportToWorldPoint(new Vector3(0f, entryViewportY, 0f));
            Vector3 right = cam.ViewportToWorldPoint(new Vector3(1f, entryViewportY, 0f));
            minX = left.x + entryXPadding;
            maxX = right.x - entryXPadding;
            topY = left.y;
            bottomBound = cam.ViewportToWorldPoint(Vector3.zero).y;

            // Entra SEMPRE dall'alto e dentro lo schermo: così discesa + telegraph sono visibili
            // (anche se l'ondata l'aveva fatto comparire fuori schermo di lato). I K-spawn restano invariati.
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = topY;
            p.z = 0f;
            transform.position = p;
        }
        else
        {
            bottomBound = -6f;
            topY = 6f;
        }

        rb.linearVelocity = Vector2.down * entrySpeed;
    }

    void Update()
    {
        // Fuori schermo (sotto): distruggi (ha mancato il dive).
        if (transform.position.y < bottomBound - offscreenMargin)
        {
            Destroy(gameObject);
            return;
        }

        if (state == State.Entering)
        {
            if (player == null) CachePlayer();
            // Aggancia solo quando è dentro lo schermo (così il telegraph è sempre visibile prima del dive).
            bool onScreen = transform.position.y <= topY
                && transform.position.x >= minX && transform.position.x <= maxX;
            if (onScreen && player != null
                && Vector2.Distance(transform.position, player.position) <= acquisitionRadius)
            {
                state = State.Telegraphing;
                StartCoroutine(TelegraphThenDive());
            }
        }
    }

    IEnumerator TelegraphThenDive()
    {
        // Pausa + lampeggio + pulsazione di scala: segnale BEN leggibile che sta per lanciarsi.
        rb.linearVelocity = Vector2.zero;

        // La scala pulsa solo sullo sprite child → la hitbox (collider sul root) resta invariata.
        Transform sprT = spriteRenderer != null ? spriteRenderer.transform : null;
        Vector3 baseScale = sprT != null ? sprT.localScale : Vector3.one;

        float t = 0f;
        bool on = false;
        while (t < telegraphDuration)
        {
            on = !on;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = on ? telegraphColor : baseColor;
                if (sprT != null) sprT.localScale = on ? baseScale * telegraphScalePunch : baseScale;
            }
            float step = Mathf.Min(telegraphBlinkInterval, telegraphDuration - t);
            yield return new WaitForSeconds(step);
            t += step;
        }
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
        if (sprT != null) sprT.localScale = baseScale;

        // Direzione bloccata sulla posizione del player ALLA FINE del telegraph → linea retta schivabile.
        if (player == null) CachePlayer();
        Vector2 dir = player != null
            ? ((Vector2)(player.position - transform.position)).normalized
            : Vector2.down;

        state = State.Diving;
        rb.linearVelocity = dir * diveSpeed;
    }

    void CachePlayer()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        player = pc != null ? pc.transform : null;
    }
}
