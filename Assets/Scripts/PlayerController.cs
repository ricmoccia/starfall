using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float maxMoveSpeed = 14f; // tetto velocità del drag: uno scatto del dito non teletrasporta
    [SerializeField] float leftBoundPadding;
    [SerializeField] float rightBoundPadding;
    [SerializeField] float upBoundPadding;
    [SerializeField] float downBoundPadding;

    Shooter playerShooter;
    InputAction moveAction;

    Camera mainCamera;
    Vector2 minBounds;
    Vector2 maxBounds;

    // Drag-to-move relativo. La nave insegue targetPos (che segue il dito 1:1) a velocità limitata.
    Vector3 lastPointerWorld;
    Vector3 targetPos;
    bool dragging;

    void Start()
    {
        playerShooter = GetComponent<Shooter>();

        // Movimento da tastiera come fallback desktop.
        moveAction = InputSystem.actions.FindAction("Move");

        mainCamera = Camera.main;
        InitBounds();
        targetPos = transform.position;

        // Auto-fire: il player spara di continuo (lo Shooter rispetta fire rate + buff Firepower).
        playerShooter.isFiring = true;
    }

    void Update()
    {
        // In pausa (Time.timeScale == 0) non muovere la nave; azzera il drag così
        // alla ripresa non c'è scatto dal punto toccato durante la pausa.
        if (Time.timeScale == 0f)
        {
            dragging = false;
            return;
        }

        MovePlayer();
    }

    void InitBounds()
    {
        minBounds = mainCamera.ViewportToWorldPoint(new Vector2(0, 0));
        maxBounds = mainCamera.ViewportToWorldPoint(new Vector2(1, 1));
    }

    void MovePlayer()
    {
        Vector3 newPos = transform.position;

        Pointer pointer = Pointer.current;
        // Un tocco/click sopra un elemento UI non deve muovere la nave.
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointer != null && pointer.press.isPressed && !overUI)
        {
            // Drag relativo 1:1: il bersaglio segue il dito dello stesso delta (in unità di mondo),
            // niente teletrasporto verso il punto toccato.
            Vector3 worldNow = PointerWorld(pointer);
            if (dragging)
            {
                targetPos += worldNow - lastPointerWorld;
            }
            else
            {
                // Primo frame del drag: il bersaglio parte dalla nave (nessuno scatto al tocco).
                targetPos = transform.position;
            }
            lastPointerWorld = worldNow;
            dragging = true;

            // Bersaglio dentro i bordi, poi la nave lo insegue a velocità limitata: uno scatto rapido
            // del dito non la teletrasporta, la raggiunge inseguendola.
            targetPos.x = Math.Clamp(targetPos.x, minBounds.x + leftBoundPadding, maxBounds.x - rightBoundPadding);
            targetPos.y = Math.Clamp(targetPos.y, minBounds.y + downBoundPadding, maxBounds.y - upBoundPadding);
            newPos = Vector3.MoveTowards(newPos, targetPos, maxMoveSpeed * Time.deltaTime);
        }
        else
        {
            dragging = false;

            // Fallback tastiera (desktop).
            Vector2 move = moveAction.ReadValue<Vector2>();
            newPos += (Vector3)move * moveSpeed * Time.deltaTime;
            // Tieni il bersaglio sulla nave: riprendendo il drag non ci sarà scatto.
            targetPos = newPos;
        }

        newPos.x = Math.Clamp(newPos.x, minBounds.x + leftBoundPadding, maxBounds.x - rightBoundPadding);
        newPos.y = Math.Clamp(newPos.y, minBounds.y + downBoundPadding, maxBounds.y - upBoundPadding);

        transform.position = newPos;
    }

    Vector3 PointerWorld(Pointer pointer)
    {
        Vector2 screenPos = pointer.position.ReadValue();
        // z = distanza dalla camera (ortografica: ininfluente per x/y, ma esplicito per chiarezza).
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
    }
}
