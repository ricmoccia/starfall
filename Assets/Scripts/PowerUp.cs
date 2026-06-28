using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Firepower, Health }

    [SerializeField] PowerUpType type;
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float bottomMargin = 1f;

    float bottomBound;

    void Start()
    {
        // Coerente con PlayerController: limiti dal viewport della camera.
        bottomBound = Camera.main.ViewportToWorldPoint(Vector3.zero).y;
    }

    void Update()
    {
        // Scende verso il basso, così il player ancorato in basso può raccoglierlo.
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        if (transform.position.y < bottomBound - bottomMargin)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerBuffs buffs = other.GetComponent<PlayerBuffs>();

        if (buffs != null)
        {
            buffs.Apply(type);
            AudioManager.Instance?.PlayPickup();
            Destroy(gameObject);
        }
    }
}
