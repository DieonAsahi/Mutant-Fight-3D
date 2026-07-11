using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Detection")]
    public float detectRange = 15f;
    public float attackRange = 2.5f;

    [Header("Attack")]
    public float attackCooldown = 2f;

    private NavMeshAgent agent;
    private Animator anim;

    private float nextAttackTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null)
            return;

        float distance =
          Vector3.Distance(transform.position, player.position);

        // Tidak mendeteksi player
        if (distance > detectRange)
        {
            agent.isStopped = true;
            anim.SetBool("isWalking", false);
            return;
        }

        // Mengejar player
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            anim.SetBool("isWalking", true);
        }
        else
        {
            agent.isStopped = true;
            anim.SetBool("isWalking", false);

            Vector3 lookPos =
              player.position - transform.position;

            lookPos.y = 0;

            transform.rotation =
              Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookPos),
                Time.deltaTime * 5f
              );

            Attack();
        }
    }

    void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        int attack = Random.Range(0, 3);

        switch (attack)
        {
            case 0:
                anim.SetTrigger("Punch");
                break;

            case 1:
                anim.SetTrigger("Swipe");
                break;

            case 2:
                anim.SetTrigger("JumpAttack");
                break;
        }
    }
}
