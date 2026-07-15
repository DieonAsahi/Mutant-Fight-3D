using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PoliceAI : MonoBehaviour
{
    [Header("Target & Status")]
    [SerializeField] private Transform playerTransform;
    private PlayerStatusManager playerStatus;

    [Header("Police Stats")]
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private bool isDead = false;
    private bool isStunned = false;

    [Header("Detection Settings")]
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float attackRange = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float damageAmount = 5f;

    private NavMeshAgent agent;
    private Animator anim;
    private bool canAttack = true;

    void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerStatus = playerObj.GetComponent<PlayerStatusManager>();
        }
    }

    void Update()
    {
        if (isDead || isStunned) return;

        if (playerStatus != null && playerStatus.IsDead)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = true;

            if (anim != null)
                anim.SetBool("isWalking", false);

            // GANTI DI SINI
            if (PoliceUIManager.Instance != null)
                PoliceUIManager.Instance.ClearActivePolice(this);

            return;
        }

        if (playerTransform == null || Time.timeScale == 0f)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectRange && distanceToPlayer > attackRange)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
            }

            if (anim != null)
                anim.SetBool("isWalking", true);

            // GANTI DI SINI
            if (PoliceUIManager.Instance != null)
                PoliceUIManager.Instance.SetActivePolice(this);
        }
        else if (distanceToPlayer <= attackRange)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = true;

            if (anim != null)
                anim.SetBool("isWalking", false);

            if (canAttack)
                StartCoroutine(AttackRoutine());
        }
        else
        {
            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = true;

            if (anim != null)
                anim.SetBool("isWalking", false);
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        if (anim != null)
            anim.SetTrigger("Punch");

        if (playerStatus != null)
        {
            bool playerIsBlocking = false;

            playerStatus.TakeDamage(damageAmount, playerIsBlocking, "Normal", "Front");

            Debug.Log($"Polisi berhasil memukul Player! Damage: {damageAmount}");
        }

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    public void TakeDamageFromPlayer(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // GANTI DI SINI
        if (PoliceUIManager.Instance != null)
        {
            PoliceUIManager.Instance.SetActivePolice(this);
            PoliceUIManager.Instance.UpdateActivePolice(this);
        }

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        isStunned = true;

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;

        if (anim != null)
            anim.SetTrigger("Hit");

        yield return new WaitForSeconds(1.0f);

        isStunned = false;
    }

    private void Die()
    {
        isDead = true;

        if (agent != null)
            agent.enabled = false;

        if (anim != null)
            anim.Play("Daying");

        // 1. Matikan script-nya TERLEBIH DAHULU agar tidak ikut terhitung
        this.enabled = false;

        if (PoliceUIManager.Instance != null)
        {
            PoliceUIManager.Instance.ClearActivePolice(this);

            // 2. Baru panggil pengecekan setelah script dipastikan mati
            PoliceUIManager.Instance.CheckRemainingPolice();
        }

        // Hancurkan objek setelah animasi selesai
        Destroy(gameObject, 2.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}