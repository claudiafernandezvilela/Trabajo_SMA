using UnityEngine;

// Capa reactiva: evalúa el estado del mundo en cada frame y establece
// prioridades inmediatas. Es la única capa que puede interrumpir al
// deliberativo con respuestas de alta prioridad.
public class CerebroReactivo : MonoBehaviour
{
    private ModeloMundo modelo;
    private CerebroDeliberativo deliberativo;
    private Cerebro cerebro;

    void Awake()
    {
        modelo = GetComponent<ModeloMundo>();
        deliberativo = GetComponent<CerebroDeliberativo>();
        cerebro = GetComponent<Cerebro>();
    }
    public void Evaluar()
    {
        // Alta: jugador visible, perseguir siempre
        if (modelo.jugadorVisible)
        {
            float distancia = Vector3.Distance(cerebro.transform.position, modelo.player.position);
            if (distancia <= cerebro.distanciaAtaque)
            {
                cerebro.AtraparJugador();
                return;
            }
            deliberativo.EstablecerObjetivo(Objetivo.Perseguir);
            return;
        }
            // Perdió al jugador que perseguía → buscar
            if (deliberativo.ObjetivoActual == Objetivo.Perseguir && !modelo.jugadorVisible)
            {
                Debug.Log("Reactivo: perdió al jugador → Buscar");
                deliberativo.EstablecerObjetivo(Objetivo.Buscar);
                return;
            }
            // Media: jugador escuchado, buscar si no hay algo más urgente
            if (modelo.jugadorEscuchado && deliberativo.ObjetivoActual != Objetivo.Perseguir)
            {
                deliberativo.EstablecerObjetivo(Objetivo.Buscar);
                modelo.ResetearEscucha();
            }
        }
    }