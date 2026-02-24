using UnityEngine;
using UnityEngine.AI;

public class DogVision : MonoBehaviour
{
    public Transform target;          // La chica
    public float viewDistance = 10f;  // Distancia máxima de visión
    public float viewAngle = 60f;     // Ángulo del cono de visión
    public NavMeshAgent agent;
    public LayerMask obstacleMask;    // Capa de paredes / obstáculos

    void Update()
    {
        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= viewDistance)
        {
            // Calcula el ángulo entre el frente del perro y el target
            float angleToTarget = Vector3.Angle(transform.forward, direction);

            if (angleToTarget <= viewAngle / 2)
            {
                // Raycast para verificar si hay obstáculos
                if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, direction.normalized, distance, obstacleMask))
                {
                    // El perro ve al target → perseguir
                    agent.SetDestination(target.position);
                    return; // sale de Update para no quedarse quieto
                }
            }
        }

        // Si no ve al target → se queda quieto
        agent.SetDestination(transform.position);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward * viewDistance;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, viewAngle / 2, 0) * forward);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -viewAngle / 2, 0) * forward);
    }
}
