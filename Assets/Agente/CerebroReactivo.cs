using UnityEngine;

// Respuestas inmediatas de alta prioridad
public class CerebroReactivo : MonoBehaviour
{
    private ModeloMundo modelo;
    private CerebroDeliberativo deliberativo;

    void Awake()
    {
        modelo = GetComponent<ModeloMundo>();
        deliberativo = GetComponent<CerebroDeliberativo>();
    }

    public void Evaluar()
    {
        // Prioridad máxima: jugador visible
        if (modelo.jugadorVisible)
        {
            deliberativo.EstablecerObjetivo(Objetivo.Perseguir);
            return;
        }

        // Prioridad media: perdió al jugador
        if (deliberativo.ObjetivoActual == Objetivo.Perseguir && !modelo.jugadorVisible)
        {
            deliberativo.EstablecerObjetivo(Objetivo.Buscar);
        }
    }
}
