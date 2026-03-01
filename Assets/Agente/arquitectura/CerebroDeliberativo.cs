using UnityEngine;

public enum Objetivo { Patrullar, Perseguir, Buscar, RevisarObjeto, AsegurarZona }

// Capa deliberativa: gestiona metas a largo plazo y procesa eventos
// que le notifican los estados a través del Cerebro.
public class CerebroDeliberativo : MonoBehaviour
{
    public Objetivo ObjetivoActual { get; private set; }

    private ModeloMundo modelo;
    private Cerebro cerebro;
    private bool yaReviso = false;

    void Awake()
    {
        modelo = GetComponent<ModeloMundo>();
        cerebro = GetComponent<Cerebro>();
        ObjetivoActual = Objetivo.Patrullar;
    }

    public void EstablecerObjetivo(Objetivo nuevoObjetivo)
    {
        if (ObjetivoActual == nuevoObjetivo) return;
        Debug.Log("Deliberativo: " + ObjetivoActual + " → " + nuevoObjetivo);
        ObjetivoActual = nuevoObjetivo;
        cerebro.CambiarEstadoPorObjetivo(nuevoObjetivo);
    }

    // Procesa los eventos notificados por el Cerebro en nombre de los Estados.
    public void ProcesarEvento(Evento evento)
    {
        switch (evento)
        {
            case Evento.BusquedaTerminada:
                Debug.Log("Deliberativo: BusquedaTerminada - objetoVigilado: " + modelo.objetoVigilado);
                if (yaReviso && modelo.objetoRobado)
                    EstablecerObjetivo(Objetivo.AsegurarZona);
                else if (modelo.objetoVigilado != null)
                    EstablecerObjetivo(Objetivo.RevisarObjeto);
                else
                    EstablecerObjetivo(Objetivo.Patrullar);
                break;

            case Evento.RevisionTerminada:
                yaReviso = true; // ya revisó
                if (modelo.objetoRobado)
                    EstablecerObjetivo(Objetivo.AsegurarZona);
                else
                    EstablecerObjetivo(Objetivo.Patrullar);
                break;

            case Evento.AsegurarZonaTerminada:
                EstablecerObjetivo(Objetivo.Patrullar);
                break;
        }
    }
}
