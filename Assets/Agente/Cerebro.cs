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

    private int IndicePatrulla;
    private IEstado estadoActual;

    private ModeloMundo modelo;
    private CerebroReactivo reactivo;
    private CerebroDeliberativo deliberativo;
    [Header("Asegurar Zona")]
public Transform[] PuntosAsegurarZona;

    void Awake()
    {
        modelo = GetComponent<ModeloMundo>();
        reactivo = GetComponent<CerebroReactivo>();
        deliberativo = GetComponent<CerebroDeliberativo>();
    }

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        CambiarEstado(new Patrulla());
        SiguientePunto();
    }

    void Update()
    {
        reactivo.Evaluar();
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
                SiguientePunto();
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
        }
    }

    public void SiguientePunto()
    {
        if (PuntosPatrullas.Length == 0) return;
        agente.destination = PuntosPatrullas[IndicePatrulla].position;
        IndicePatrulla = (IndicePatrulla + 1) % PuntosPatrullas.Length;
    }

    public void AtraparJugador()
    {
        Debug.Log("¡Jugador atrapado! Game Over");
        Time.timeScale = 0f;
    }

    // Sensor callbacks
    public void OnPlayerSeen(Transform playerTransform)
    {
        modelo.ActualizarJugadorVisto(playerTransform);
    }

    public void OnPlayerLost()
    {
        modelo.ActualizarJugadorPerdido();
    }

    public void OnPlayerHeard(Vector3 soundPosition)
    {
        if (deliberativo.ObjetivoActual == Objetivo.Perseguir) return;
        modelo.ActualizarSonido(soundPosition);
        if (deliberativo.ObjetivoActual != Objetivo.Buscar)
            deliberativo.EstablecerObjetivo(Objetivo.Buscar);
    }
}