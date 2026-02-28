using UnityEngine;
using UnityEngine.AI;

public class Cerebro : MonoBehaviour
{
    [Header("Search Settings")]
    public float TiempoBusqueda = 5f;

    [Header("References")]
    public NavMeshAgent agente;
    public Transform[] PuntosPatrullas;

    [Header("Memory")]
    public Vector3 UltimaPosicion;
    public bool JugadorVisible;
    public bool JugadorEscuchado;

    // Accesos directos al mundo
    public Transform player     => Mundo.Instancia.player;
    public Transform objetoVigilado => Mundo.Instancia.objetoVigilado;
    public Vector3 posicionObjeto   => Mundo.Instancia.posicionObjeto;

    [Header("Attack Settings")]
    public float distanciaAtaque = 1.5f;
    private int IndicePatrulla;
    private IEstado estadoActual;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        CambiarEstado(new Patrulla());
        SiguientePunto();
    }

    void Update()
    {
        EvaluarTransiciones();
        estadoActual?.Ejecutar(this);
    }

    void EvaluarTransiciones()
    {
        Debug.Log("Estado actual: " + estadoActual.GetType().Name);

        // PRIORIDAD MÁXIMA: jugador visible
        if (JugadorVisible)
        {
            if (!(estadoActual is Perseguir))
                CambiarEstado(new Perseguir());

            return;
        }

        // Si estaba persiguiendo y lo pierde
        if (estadoActual is Perseguir && !JugadorVisible)
        {
            UltimaPosicion = player.position;
            CambiarEstado(new Buscar());
            return;
        }
    }

    public void CambiarEstado(IEstado nuevoEstado)
    {
        estadoActual = nuevoEstado;
    }

    public void SiguientePunto()
    {
        if (PuntosPatrullas.Length == 0) return;
        agente.destination = PuntosPatrullas[IndicePatrulla].position;
        IndicePatrulla = (IndicePatrulla + 1) % PuntosPatrullas.Length;
    }

    public void BusquedaTerminada()
    {
        if (objetoVigilado != null)
            CambiarEstado(new RevisarObjeto());
        else
            CambiarEstado(new Patrulla());
    }
    public void RevisionTerminada()
    {
        if (Mundo.Instancia.objetoRobado)
            CambiarEstado(new Buscar());
        else
            CambiarEstado(new Patrulla());
    }


    public void OnPlayerSeen(Transform playerTransform)
    {
        Mundo.Instancia.player = playerTransform;
        JugadorVisible = true;
    }

    public void OnPlayerLost()
    {
        JugadorVisible = false;
    }

    public void OnPlayerHeard(Vector3 soundPosition)
    {
        if (estadoActual is Perseguir)
            return;

        UltimaPosicion = soundPosition;

        if (!(estadoActual is Buscar))
            CambiarEstado(new Buscar());
    }


    public void AtraparJugador()
    {
        Debug.Log("¡Jugador atrapado! Game Over");
        Time.timeScale = 0f;
    }
}

