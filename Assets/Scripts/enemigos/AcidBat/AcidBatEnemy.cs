using System.Collections;
using UnityEngine;

public class AcidBatEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject acidProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDelay = 0.5f;  // espera antes de disparar para animación
    [SerializeField] private string attack1Trigger = "ATTACK";
    [SerializeField] private string attack2Trigger = "ATTACK2";

    [Header("Animation")]
    [SerializeField] private string moveBool = "MOVE";

    private bool useAttack1 = true;
    private float lastAttackTime = -999f;

    private void Update()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
            if (playerTarget == null) return;
        }

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        // Mover hacia el jugador si aún no está dentro del rango de ataque
        if (dist > stopDistance)
        {
            animator.SetBool(moveBool, true);
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool(moveBool, false);
            TryBeginAttack();
        }
    }

    private void TryBeginAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        // Alternar animación
        string trigger = useAttack1 ? attack1Trigger : attack2Trigger;
        useAttack1 = !useAttack1;

        if (animator) animator.SetTrigger(trigger);

        // Iniciar la rutina que espera antes de disparar
        PerformAcidAttack(playerTarget.position);
    }

    private void PerformAcidAttack(Vector2 targetPos)
    {
        StartCoroutine(DelayedShoot(targetPos));
    }

    private IEnumerator DelayedShoot(Vector2 targetPos)
    {
        yield return new WaitForSeconds(attackDelay);

        if (acidProjectilePrefab != null && projectileSpawnPoint != null)
        {
            GameObject proj = Instantiate(acidProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            var ap = proj.GetComponent<AcidProjectile>();
            if (ap != null)
                ap.Init(targetPos);
        }
    }
}
