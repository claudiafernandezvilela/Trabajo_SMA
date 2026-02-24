using UnityEngine;

public class Guardia : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    private int currentPatrolIndex;
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform player;
    private bool isChasing = false;
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        GoToNextPatrolPoint();
    }

    void Update()
    {
                if (CanSeePlayer())
        {
            isChasing = true;
        }

        if (isChasing)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            Perseguir();
        }
    }
    void Perseguir()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPatrolPoint();
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.destination = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // 1️⃣ Distancia
        if (Vector3.Distance(transform.position, player.position) > visionRange)
            return false;

        // 2️⃣ Ángulo (cono frontal)
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > visionAngle / 2)
            return false;

        // 3️⃣ Raycast (paredes)
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f,
                            directionToPlayer,
                            out RaycastHit hit,
                            visionRange))
        {
            if (hit.transform.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    // Dibuja el cono en escena
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftBoundary * visionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * visionRange);
    }
}
