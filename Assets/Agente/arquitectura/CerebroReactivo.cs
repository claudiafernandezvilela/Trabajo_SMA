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
    public void OnPlayerSeen(Transform playerTransform)
{
    float distancia = Vector3.Distance(cerebro.transform.position, playerTransform.position);
    if (distancia <= cerebro.distanciaAtaque)
    {
        cerebro.AtraparJugador();
        return;
    }
    deliberativo.EstablecerObjetivo(Objetivo.Perseguir);
}

public void OnPlayerLost()
{
    if (deliberativo.ObjetivoActual == Objetivo.Perseguir)
    {
        Debug.Log("Reactivo: perdió al jugador → Buscar");
        deliberativo.EstablecerObjetivo(Objetivo.Buscar);
    }
}

public void OnPlayerHeard(Vector3 soundPosition)
{
    if (deliberativo.ObjetivoActual != Objetivo.Perseguir)
    {
        deliberativo.EstablecerObjetivo(Objetivo.Buscar);
    }
}

    }