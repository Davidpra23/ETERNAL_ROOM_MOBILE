using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private float pickupDelay = 0.5f; // ⏱️ Tiempo antes de poder recogerla

    [Header("Attraction Settings")]
    [SerializeField] private float attractionRange = 2.5f;   // Distancia a la que empieza a atraerse
    [SerializeField] private float attractionSpeed = 6f;     // Velocidad de atracción

    private Transform player;          // Referencia al jugador
    private bool canBeCollected = false; // Controla si ya puede recogerse

    private void Start()
    {
        // Busca al jugador automáticamente
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Inicia el delay de recolección
        StartCoroutine(EnablePickupAfterDelay());
    }

    private IEnumerator EnablePickupAfterDelay()
    {
        canBeCollected = false;
        yield return new WaitForSeconds(pickupDelay);
        canBeCollected = true;
    }

    private void Update()
    {
        // Si hay jugador y ya puede ser recogida, empieza la atracción
        if (player != null && canBeCollected)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance < attractionRange)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                transform.position += (Vector3)(direction * attractionSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo puede recogerse si ha pasado el delay
        if (canBeCollected && other.CompareTag("Player"))
        {
            ScoreManager.Instance?.AddScore(coinValue);

            if (pickupEffectPrefab != null)
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
