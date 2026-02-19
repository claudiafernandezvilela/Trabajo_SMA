using UnityEngine;

public class MovGuardia : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform[] puntosPatrulla;
    public Transform ladron;
    public float rangoVision;

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
        float distancia = Vector3.Distance(transform.position, ladron.position);

        // Cambio de estado
        if (distancia < rangoVision)
        {
            estadoActual = Estado.Perseguir;
        }
        else if (estadoActual == Estado.Perseguir && distancia >= rangoVision)
        {
            estadoActual = Estado.Patrulla;
            agent.SetDestination(puntosPatrulla[indiceActual].position);
        }

        // Ejecutar estado
        switch (estadoActual)
        {
            case Estado.Patrulla:
                Patrullar();
                break;

            case Estado.Perseguir:
                Perseguir();
                break;
        }
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
}
