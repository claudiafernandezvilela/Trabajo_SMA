using UnityEngine;
using UnityEngine.AI;

public class Cerebro : MonoBehaviour
{
    [Header("Search Settings")]
    public float TiempoBusqueda = 5f;

    [Header("References")]
    public NavMeshAgent agente;
    public Transform[] PuntosPatrullas;

    [Header("Attack Settings")]
    public float distanciaAtaque = 1.5f;
    [Header("Asegurar Zona")]
    public Transform[] PuntosAsegurarZona;
    public ModeloMundo Modelo { get; private set; }
    public CerebroDeliberativo Deliberativo { get; private set; }
    private int IndicePatrulla;
    private IEstado estadoActual;
    private CerebroReactivo reactivo;

    void Awake()
    {
        Modelo = GetComponent<ModeloMundo>();
        reactivo = GetComponent<CerebroReactivo>();
        Deliberativo = GetComponent<CerebroDeliberativo>();
        agente = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        CambiarEstado(new Patrulla());
    }

    void Update()
    {
        estadoActual?.Ejecutar(this);
    }

    public void CambiarEstado(IEstado nuevoEstado)
    {
        estadoActual = nuevoEstado;
    }

    public void CambiarEstadoPorObjetivo(Objetivo objetivo)
    {
        switch (objetivo)
        {
            case Objetivo.Patrullar:
                CambiarEstado(new Patrulla());
                break;
            case Objetivo.Perseguir:
                CambiarEstado(new Perseguir());
                break;
            case Objetivo.Buscar:
                CambiarEstado(new Buscar());
                break;
            case Objetivo.RevisarObjeto:
                CambiarEstado(new RevisarObjeto());
                break;
            case Objetivo.AsegurarZona:
                CambiarEstado(new AsegurarZona());
                break;

        }
    }

    // Notificación sensores 

    public void NotificarEvento(Evento evento)
    {
        Deliberativo.ProcesarEvento(evento);
    }

    public void OnPlayerSeen(Transform playerTransform)
    {
        Modelo.ActualizarJugadorVisto(playerTransform);
        reactivo.OnPlayerSeen(playerTransform);
    }

    public void OnPlayerLost()
    {
        Modelo.ActualizarJugadorPerdido();
        reactivo.OnPlayerLost();
    }

    public void OnPlayerHeard(Vector3 soundPosition)
    {
        Modelo.ActualizarSonido(soundPosition);
        reactivo.OnPlayerHeard(soundPosition); 
    }

public void ObjetoRobado()
{
    Modelo.objetoRobado = true;
}
    // Fin del juego
    public void AtraparJugador()
    {
        Debug.Log("¡Jugador atrapado! Game Over");
        Time.timeScale = 0f;
    }
}