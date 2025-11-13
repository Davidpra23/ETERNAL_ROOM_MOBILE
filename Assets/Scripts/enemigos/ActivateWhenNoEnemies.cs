using UnityEngine;

public class ActivateWhenNoEnemies : MonoBehaviour
{
    [Header("Objeto que se activará")]
    [SerializeField] private GameObject objectToActivate;

    [Header("Intervalo de comprobación (segundos)")]
    [SerializeField] private float checkInterval = 1f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckEnemies();
        }
    }

    void CheckEnemies()
    {
        // Busca todos los objetos con el tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        // Activa si no hay enemigos, desactiva si hay
        bool noEnemies = enemies.Length == 0;
        if (objectToActivate != null)
            objectToActivate.SetActive(noEnemies);
    }
}
