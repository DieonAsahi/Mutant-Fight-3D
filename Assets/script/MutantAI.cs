using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class MutantAI : MonoBehaviour
{
    [Header("Target & Status")]
    [SerializeField] private Transform playerTransform;
    private PlayerStatusManager playerStatus;
    private PlayerTPS playerMovement;

    [Header("Mutant Stats")]
    [SerializeField] private float maxHealth = 80f; // Request darah 80
    private float currentHealth;
    private bool isDead = false;
    private bool isStunned = false;

    [Header("Detection Settings")]
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float attackRange = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float damageAmount = 5f;

    private NavMeshAgent agent;
    private Animator anim;
    private float nextAttackTime;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;

        if (playerTransform != null)
        {
            playerStatus = playerTransform.GetComponent<PlayerStatusManager>();
            playerMovement = playerTransform.GetComponent<PlayerTPS>();
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerStatus = playerObj.GetComponent<PlayerStatusManager>();
                playerMovement = playerObj.GetComponent<PlayerTPS>();
            }
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Pengecekan jika Player sudah mati
        if (playerStatus != null && playerStatus.IsDead)
        {
            StopEnemy();
            if (MutantUIManager.Instance != null) MutantUIManager.Instance.ClearActiveMutant(this);
            return;
        }

        // ---- KODE FIKS DETEKSI DI SINI ----
        if (playerTransform != null)
        {
            // Hitung jarak antara Mutant dan Player
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Jika player masuk dalam jarak deteksi (12f)
            if (distanceToPlayer <= detectRange)
            {
                if (MutantUIManager.Instance != null)
                {
                    MutantUIManager.Instance.SetActiveMutant(this);
                }
            }
        }
        // ----------------------------------

        if (playerTransform == null || Time.timeScale == 0f || isStunned)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 1. KONDISI: Player di luar jangkauan deteksi (Slider Hilang)
        if (distance > detectRange)
        {
            StopEnemy();
            if (MutantUIManager.Instance != null)
            {
                MutantUIManager.Instance.ClearActiveMutant(this);
            }
            return;
        }

        // Jika masuk radar deteksi, daftarkan diri ke UI Slider agar bar muncul
        if (MutantUIManager.Instance != null && !isDead)
        {
            MutantUIManager.Instance.SetActiveMutant(this);
        }

        // 2. KONDISI: Kejar Player
        if (distance > attackRange)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
            }
            if (anim != null) anim.SetBool("isWalking", true);
        }
        // 3. KONDISI: Masuk jarak pukul
        else
        {
            StopEnemy();
            RotateTowardsPlayer();
            TryAttack();
        }
    }

    // FUNGSI UTK MENERIMA DAMAGE DARI PLAYER
    public void TakeDamageFromPlayer(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Langsung rebut fokus Slider UI ke mutant yang baru saja kena hit ini!
        if (MutantUIManager.Instance != null)
        {
            // Paksa aktifkan dan update nilainya saat dipukul
            MutantUIManager.Instance.SetActiveMutant(this);
            MutantUIManager.Instance.UpdateActiveMutant(this);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Picu animasi Hit
            StartCoroutine(HitReactionRoutine());
        }
    }

    private IEnumerator HitReactionRoutine()
    {
        isStunned = true;
        StopEnemy();

        if (anim != null) anim.SetTrigger("Hit");

        // Durasi hit diperlama menjadi 1.5 detik (meningkat dari sebelumnya)
        yield return new WaitForSeconds(1.5f);
        isStunned = false;
    }

    private void Die()
    {
        isDead = true;
        StopEnemy();

        if (anim != null) anim.SetTrigger("Dying"); // Pemicu trigger Daying di Animator
        if (agent != null) agent.enabled = false;   // Matikan Navmesh agar tidak bergeser

        if (MutantUIManager.Instance != null)
        {
            MutantUIManager.Instance.ClearActiveMutant(this);
        }

        // Panggil Coroutine untuk menghitung mundur durasi animasi mati secara akurat
        StartCoroutine(DestroyAfterDeathAnimation());
    }

    private IEnumerator DestroyAfterDeathAnimation()
    {
        // Tunggu 1 frame agar Animator sempat berpindah ke state Dying/Daying
        yield return null;

        if (anim != null)
        {
            // Ambil informasi state animasi yang sedang berjalan di layer 0
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            // Ambil durasi total dari animasi tersebut (dalam hitungan detik)
            float deathAnimDuration = stateInfo.length;

            // Tunggu sampai animasi selesai dimainkan sepenuhnya
            yield return new WaitForSeconds(deathAnimDuration);
        }
        else
        {
            // Failsafe jika animator ternyata null
            yield return new WaitForSeconds(2f);
        }

        // Hancurkan objek mutant dari scene
        Destroy(gameObject);
    }

    private void StopEnemy()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (anim != null) anim.SetBool("isWalking", false);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 lookPos = playerTransform.position - transform.position;
        lookPos.y = 0;
        if (lookPos != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 7f);
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || isStunned) return;

        nextAttackTime = Time.time + attackCooldown;
        if (anim != null) anim.SetTrigger("Punch");

        if (playerStatus != null)
        {
            bool playerIsBlocking = CheckIfPlayerIsBlocking();
            playerStatus.TakeDamage(damageAmount, playerIsBlocking);
        }
    }

    private bool CheckIfPlayerIsBlocking()
    {
        if (playerMovement != null)
        {
            return playerMovement.IsPlayerBlocking;
        }
        return false;
    }

    // Fungsi pembantu untuk dibaca oleh script UIManager
    public float GetCurrentHealth() { return currentHealth; }
    public bool IsDeadEnemy() { return isDead; }
}