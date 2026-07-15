using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    [Header("Target & Status")]
    public Transform player;
    private PlayerStatusManager playerStatus;

    [Header("Boss Stats")]
    public float maxHealth = 200f;
    public float currentHealth;
    private bool isDead = false;
    private bool isStunned = false;

    [Header("Detection Settings")]
    public float detectRange = 15f;
    public float attackRange = 2.5f;

    [Header("Attack Settings")]
    public float attackCooldown = 2f;
    private float nextAttackTime;

    private NavMeshAgent agent;
    private Animator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player != null)
        {
            playerStatus = player.GetComponent<PlayerStatusManager>();
        }
    }

    void Update()
    {
        if (isDead || isStunned || player == null || Time.timeScale == 0f) return;

        if (playerStatus != null && playerStatus.IsDead)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (anim != null) anim.SetBool("isWalking", false);
            if (BossUIManager.Instance != null) BossUIManager.Instance.ClearActiveBoss(this);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (anim != null) anim.SetBool("isWalking", false);
            return;
        }

        if (distance > attackRange)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            if (anim != null) anim.SetBool("isWalking", true);

            // Aktifkan bar darah bos saat player masuk jarak pandang
            if (BossUIManager.Instance != null) BossUIManager.Instance.SetActiveBoss(this);
        }
        else
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (anim != null) anim.SetBool("isWalking", false);

            Vector3 lookPos = player.position - transform.position;
            lookPos.y = 0;
            if (lookPos != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), Time.deltaTime * 5f);
            }

            if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
    }

    void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        int attackType = Random.Range(0, 3);
        float damageAmount = 0f;

        switch (attackType)
        {
            case 0:
                if (anim != null) anim.SetTrigger("Punch");
                damageAmount = 10f;
                break;
            case 1:
                if (anim != null) anim.SetTrigger("Swipe");
                damageAmount = 25f;
                break;
            case 2:
                if (anim != null) anim.SetTrigger("JumpAttack");
                damageAmount = 50f;
                break;
        }

        if (playerStatus != null)
        {
            bool playerIsBlocking = false;
            playerStatus.TakeDamage(damageAmount, playerIsBlocking, "Normal", "Front");
        }
    }

    public void TakeDamageFromPlayer(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Update slider darah bos saat dipukul
        if (BossUIManager.Instance != null)
        {
            BossUIManager.Instance.SetActiveBoss(this);
            BossUIManager.Instance.UpdateActiveBoss(this);
        }

        if (currentHealth <= 0) Die();
        else StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        isStunned = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (anim != null) anim.SetTrigger("Hit");
        yield return new WaitForSeconds(0.8f);
        isStunned = false;
    }

    private void Die()
    {
        isDead = true;
        if (agent != null) agent.enabled = false;

        // Gunakan parameter string jika trigger "Die" tidak merespon di Animator
        if (anim != null) anim.SetTrigger("Die");

        // 1. Matikan script bos ini terlebih dahulu agar aman
        this.enabled = false;

        // 2. Beritahu BossUIManager bahwa bos telah mati
        if (BossUIManager.Instance != null)
        {
            // Panggil UpdateActiveBoss agar UIManager tahu darahnya sudah <= 0
            BossUIManager.Instance.UpdateActiveBoss(this);
        }

        // 3. Hancurkan objek setelah animasi selesai (3 detik)
        Destroy(gameObject, 3.0f);
    }
}