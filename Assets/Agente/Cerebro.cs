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
    public Transform player;
    public Vector3 UltimaPosicion;
    public bool JugadorVisible;
    public bool JugadorEscuchado;
    [Header("Attack Settings")]
    public float distanciaAtaque = 1.5f;
    [Header("Object Settings")]
    public Transform objetoVigilado;
    public Vector3 posicionObjeto;

    private int IndicePatrulla;
    private IEstado estadoActual;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        if (objetoVigilado != null) posicionObjeto = objetoVigilado.position;
        CambiarEstado(new Patrulla());
        SiguientePunto();
    }

    void Update()
    {
        estadoActual?.Ejecutar(this);
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

    public void OnPlayerSeen(Transform playerTransform)
    {
        player = playerTransform;
        JugadorVisible = true;
    }

    public void OnPlayerLost()
    {
        JugadorVisible = false;
    }

    public void OnPlayerHeard(Vector3 soundPosition)
    {
        if (estadoActual is Perseguir) return;
        UltimaPosicion = soundPosition;
        CambiarEstado(new Buscar());
    }

    public void AtraparJugador()
{
    Debug.Log("¡Jugador atrapado! Game Over");
    // O parar el juego en el editor:
    Time.timeScale = 0f;
}
    }

