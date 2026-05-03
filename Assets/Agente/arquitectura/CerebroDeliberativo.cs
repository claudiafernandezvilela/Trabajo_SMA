using UnityEngine;

public enum Objetivo { Patrullar, Perseguir, Buscar, RevisarObjeto, AsegurarZona, BloquearSalida }

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

    // Actualiza el objetivo sin cambiar el estado de comportamiento.
    // Usado cuando el Cerebro ya instanció directamente el estado (e.g. BloquearSalida).
    public void ForzarObjetivo(Objetivo nuevoObjetivo)
    {
        Debug.Log("Deliberativo: " + ObjetivoActual + " → " + nuevoObjetivo);
        ObjetivoActual = nuevoObjetivo;
    }

    // Procesa los eventos notificados por el Cerebro en nombre de los Estados.
    public void ProcesarEvento(Evento evento)
    {
        switch (evento)
        {
            case Evento.BusquedaTerminada:
                cerebro.NotificarTareaContractNetCompletada();
                Debug.Log("Deliberativo: BusquedaTerminada - objetoVigilado: " + modelo.objetoVigilado);
                if (modelo.objetoRobado)
                    EstablecerObjetivo(Objetivo.AsegurarZona);
                else if (modelo.objetoVigilado != null)
                    EstablecerObjetivo(Objetivo.RevisarObjeto);
                else
                    EstablecerObjetivo(Objetivo.Patrullar);
                break;

            case Evento.RevisionTerminada:
                if (modelo.objetoRobado){
                    yaReviso = true;
                    cerebro.NotificarObjetoRobado();
                    EstablecerObjetivo(Objetivo.AsegurarZona);
                    // Pedir a un agente libre que también asegure otra zona.
                    cerebro.IniciarRequestAsegurar();}
                else
                    EstablecerObjetivo(Objetivo.Patrullar);
                break;

            case Evento.AsegurarZonaTerminada:
                // Si este agente aceptó un Request de asegurar zona, notificar InformDone.
                cerebro.NotificarAsegurarZonaCompletada();
                EstablecerObjetivo(Objetivo.Patrullar);
                break;

            case Evento.BloquearSalidaTerminado:
                cerebro.NotificarTareaContractNetCompletada();
                break;
        }
    }
}