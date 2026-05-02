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
    private CapaComunicacion comunicacion;

    void Awake()
    {
        Modelo = GetComponent<ModeloMundo>();
        reactivo = GetComponent<CerebroReactivo>();
        Deliberativo = GetComponent<CerebroDeliberativo>();
        comunicacion = GetComponent<CapaComunicacion>();
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
        comunicacion.NotificarLadronVisto(playerTransform.position);
    }

    public void OnPlayerLost()
    {
        Modelo.ActualizarJugadorPerdido();
        reactivo.OnPlayerLost();
        comunicacion.NotificarLadronPerdido();
    }

    public void OnPlayerHeard(Vector3 soundPosition)
    {
        Modelo.ActualizarSonido(soundPosition);
        reactivo.OnPlayerHeard(soundPosition); 
        comunicacion.NotificarLadronEscuchado(soundPosition);
    }

    public void ObjetoRobado()
    {
        Modelo.objetoRobado = true;
    }
    
    public void NotificarObjetoRobado()
    {
        comunicacion.NotificarObjetoRobadoBroadcast();
    }

    /// Recibe el destino calculado por el gestor y activa BloquearSalida directamente.
    public void CambiarABloquearSalida(Vector3 destino)
    {
        Deliberativo.ForzarObjetivo(Objetivo.BloquearSalida);
        CambiarEstado(new BloquearSalida(destino));
    }

    /// Cierra la conversación ContractNet activa enviando InformDone al gestor.
    /// Llamado por CerebroDeliberativo cuando un estado de tarea ContractNet termina.
    public void NotificarTareaContractNetCompletada()
    {
        comunicacion.NotificarTareaCompletada();
    }

    // Fin del juego
    public void AtraparJugador()
    {
        Debug.Log("¡Jugador atrapado! Game Over");
        Time.timeScale = 0f;
    }
}