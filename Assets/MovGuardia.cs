using UnityEngine;

public class MovGuardia : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform[] puntosPatrulla;
    public Transform ladron;
    public float rangoVision;
    public float anguloVision;
    public LayerMask capaObstaculos;

    private UnityEngine.AI.NavMeshAgent agent;
    private int indiceActual = 0;

    private enum Estado { Patrulla, Perseguir }
    private Estado estadoActual;
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        estadoActual = Estado.Patrulla;

        if (puntosPatrulla.Length > 0)
        {
            agent.SetDestination(puntosPatrulla[indiceActual].position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (estadoActual)
        {
            case Estado.Patrulla:

                if (Vision())
                {
                    estadoActual = Estado.Perseguir;
                    break;
                }

                Patrullar();
                break;

            case Estado.Perseguir:

                if (!Vision())
                {
                    estadoActual = Estado.Patrulla;
                    agent.SetDestination(puntosPatrulla[indiceActual].position);
                    break;
                }

                Perseguir();
                break;
        }
    }
    bool Vision()
    {
         Vector3 direccionAlLadron = (ladron.position - transform.position).normalized;

        // Comprobar distancia
        float distancia = Vector3.Distance(transform.position, ladron.position);
        if (distancia > rangoVision)
            return false;

        // Comprobar ángulo (cono de visión)
        float angulo = Vector3.Angle(transform.forward, direccionAlLadron);
        if (angulo > anguloVision / 2)
            return false;

        // Raycast para comprobar paredes
        if (Physics.Raycast(transform.position, direccionAlLadron, distancia, capaObstaculos))
            return false;

        return true;
    }
    void Patrullar()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            indiceActual = (indiceActual + 1) % puntosPatrulla.Length;
            agent.SetDestination(puntosPatrulla[indiceActual].position);
        }
    }

    void Perseguir()
    {
        agent.SetDestination(ladron.position);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoVision);

        Vector3 izquierda = Quaternion.Euler(0, -anguloVision / 2, 0) * transform.forward;
        Vector3 derecha = Quaternion.Euler(0, anguloVision / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + izquierda * rangoVision);
        Gizmos.DrawLine(transform.position, transform.position + derecha * rangoVision);
    }

}
