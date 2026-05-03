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

    public virtual void OnPlayerSeen(Transform playerTransform)
    {
        Modelo.ActualizarJugadorVisto(playerTransform);
        reactivo.OnPlayerSeen(playerTransform);
        comunicacion.NotificarLadronVisto(playerTransform.position);
    }

    public virtual void OnPlayerLost()
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

    public virtual void ObjetoRobado()
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
    /// Combina el historial de Informs con la predicción de ModeloMundo para obtener la posición donde probablemente esté el ladrón ahora.
    public Vector3 ObtenerPosicionPredichaLadron()
    {
        var hist = comunicacion.Mensajes.ObtenerHistorialPosicionesLadron(3);
        return Modelo.PredecirPosicionLadron(hist);
    }

    /// Inicia una conversación QueryIf preguntando a otros agentes si ven al ladrón.
    /// Llamado por Buscar cuando expira el timer sin encontrarlo.
    public void IniciarQuery()
    {
        comunicacion.IniciarQuery();
    }

    /// Callback de CapaComunicacion con el resultado del QueryIf.
    /// Si alguien lo vio, reinicia Buscar hacia la nueva posición; si no, termina la búsqueda.
    public void OnResultadoQueryBusqueda(bool encontrado, Vector3 posicion)
    {
        if (Deliberativo.ObjetivoActual != Objetivo.Buscar) return;
        if (encontrado)
        {
            Modelo.ActualizarSonido(posicion);
            CambiarEstado(new Buscar());
        }
        else
        {
            Deliberativo.ProcesarEvento(Evento.BusquedaTerminada);
        }
    }

    /// Inicia un Request pidiendo a otro agente libre que también asegure la zona.
    /// Llamado por CerebroDeliberativo cuando se confirma que el objeto fue robado.
    public void IniciarRequestAsegurar()
    {
        comunicacion.IniciarRequestAsegurar();
    }

    /// Notifica a CapaComunicacion que AsegurarZona terminó,
    /// para que el receptor del Request pueda enviar InformDone.
    public void NotificarAsegurarZonaCompletada()
    {
        comunicacion.NotificarAsegurarZonaCompletada();
    }

    // Fin del juego
    public void AtraparJugador()
    {
        Debug.Log("¡Jugador atrapado! Game Over");
        Time.timeScale = 0f;
    }
}