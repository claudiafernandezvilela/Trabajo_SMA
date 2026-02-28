using UnityEngine;
using UnityEngine.AI;

public class PerroPuertas : MonoBehaviour
{
    public float distanciaDeteccion = 1.5f;
    public LayerMask Puerta;
    private NavMeshAgent agente;
    private AbrirPuerta puertaActual;
    private bool esperandoPuerta = false;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (esperandoPuerta)
        {
            if (puertaActual != null && puertaActual.EstaAbierta)
            {
                // La puerta ya está abierta, continuar
                esperandoPuerta = false;
                agente.isStopped = false;
            }
            return;
        }

        DetectarPuertaEnCamino();
    }

    void DetectarPuertaEnCamino()
    {
        // Lanzar un rayo en la dirección que se mueve el agente
        if (agente.velocity.magnitude < 0.1f) return;

        Vector3 direccion = agente.velocity.normalized;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                            direccion,
                            out RaycastHit hit,
                            distanciaDeteccion,
                            Puerta))
        {
            AbrirPuerta puerta = hit.collider.GetComponent<AbrirPuerta>();
        {
            if (puerta.EstaAbierta)
            {
                // Ya está abierta, pasar sin esperar
                return;
            }
            else
            {
                // Está cerrada, abrir y esperar
                puertaActual = puerta;
                puerta.Abrir();
                agente.isStopped = true;
                esperandoPuerta = true;
            }
        }
        }
    }

    void OnDrawGizmos()
    {
        if (GetComponent<NavMeshAgent>() == null) return;
        NavMeshAgent ag = GetComponent<NavMeshAgent>();
        if (ag.velocity.magnitude < 0.1f) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                       ag.velocity.normalized * 1.5f);
    }
}
