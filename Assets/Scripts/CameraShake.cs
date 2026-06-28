using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] float shakeDuration = 0.5f;   // default per il Play() senza argomenti
    [SerializeField] float shakeMagnitude = 0.5f;

    Vector3 initialPosition;
    float currentMagnitude;
    float shakeEndTime;
    Coroutine routine;

    void Start()
    {
        initialPosition = transform.position;
    }

    public void Play()
    {
        Play(shakeMagnitude, shakeDuration);
    }

    // Overlap-safe: più chiamate ravvicinate (es. tante esplosioni nemiche) prendono la
    // magnitudo/durata massima e un unico reset finale, senza "scatti".
    public void Play(float magnitude, float duration)
    {
        currentMagnitude = Mathf.Max(currentMagnitude, magnitude);
        shakeEndTime = Mathf.Max(shakeEndTime, Time.time + duration);
        if (routine == null)
        {
            routine = StartCoroutine(ShakeLoop());
        }
    }

    IEnumerator ShakeLoop()
    {
        while (Time.time < shakeEndTime)
        {
            transform.position = initialPosition + (Vector3)(Random.insideUnitCircle * currentMagnitude);
            yield return null;
        }
        transform.position = initialPosition;
        currentMagnitude = 0f;
        routine = null;
    }
}
