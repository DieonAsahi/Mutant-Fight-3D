using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MutantAI : MonoBehaviour
{
    [Header("Target & Status")]
    [SerializeField] private string playerTag = "Player";
    private Transform playerTransform;
    private PlayerStatusManager playerStatus;
    private PlayerTPS playerMovement;

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
        anim = GetComponent<Animator>();

        // Otomatis mencari player di dalam map berdasarkan Tag
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerStatus = playerObj.GetComponent<PlayerStatusManager>();
            playerMovement = playerObj.GetComponent<PlayerTPS>();
        }
    }

    private void Update()
    {
        if (playerTransform == null || Time.timeScale == 0f)
        {
            agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 1. KONDISI: Player di luar jangkauan deteksi
        if (distance > detectRange)
        {
            StopEnemy();
            return;
        }

        // 2. KONDISI: Kejar Player
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            anim.SetBool("isWalking", true);
        }
        // 3. KONDISI: Masuk jarak pukul
        else
        {
            StopEnemy();
            RotateTowardsPlayer();
            TryAttack();
        }
    }

    private void StopEnemy()
    {
        agent.isStopped = true;
        anim.SetBool("isWalking", false);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 lookPos = playerTransform.position - transform.position;
        lookPos.y = 0; // Kunci rotasi Y agar mutant tidak mendongak aneh
        if (lookPos != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 7f);
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        // Set cooldown serangan berikutnya
        nextAttackTime = Time.time + attackCooldown;

        // Picu animasi pukulan di Animator Mutant
        anim.SetTrigger("Punch");

        // KIRIM DAMAGE KE PLAYER
        if (playerStatus != null)
        {
            // Ambil status apakah player saat ini sedang menahan tombol block via input di PlayerTPS
            // Kita perlu sedikit trik: buat properti cek block di PlayerTPS atau gunakan variabel lokal
            // Karena di script sebelumnya kita punya 'isBlocking', mari kita validasi damage-nya.

            // Catatan: Pastikan di script PlayerTPS kamu, variabel 'isBlocking' dijadikan public properti atau 
            // kita bisa baca status lewat fungsi pencatat yang ada di PlayerStatusManager.

            // Mengirim damage ke PlayerStatusManager dengan membaca apa player sedang block aman
            // Kita asumsikan sistem mendeteksi jika player menahan shield statusnya terdaftar di system
            bool playerIsBlocking = false;

            // Mengambil status block dari system component di objek player
            var inputDetect = playerTransform.GetComponent<System.Reflection.FieldInfo>();

            // Jalur aman: Panggil fungsi TakeDamage. Logika pengurangan tameng/darah diatur otomatis oleh PlayerStatusManager!
            // Kita bisa cek apakah tombol block player aktif dengan membaca status tameng hancur atau tidak
            playerStatus.TakeDamage(damageAmount, CheckIfPlayerIsBlocking());
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
}