using UnityEngine;

public enum Objetivo { Patrullar, Perseguir, Buscar, RevisarObjeto }

public class CerebroDeliberativo : MonoBehaviour
{
    public Objetivo ObjetivoActual { get; private set; }

    private ModeloMundo modelo;
    private Cerebro cerebro;

    void Awake()
    {
        modelo = GetComponent<ModeloMundo>();
        cerebro = GetComponent<Cerebro>();
        ObjetivoActual = Objetivo.Patrullar;
    }

    public void EstablecerObjetivo(Objetivo nuevoObjetivo)
    {
        if (ObjetivoActual == nuevoObjetivo) return;
        ObjetivoActual = nuevoObjetivo;
        cerebro.CambiarEstadoPorObjetivo(nuevoObjetivo);
    }

    public void BusquedaTerminada()
    {
        Debug.Log("BusquedaTerminada - objetoVigilado: " + modelo.objetoVigilado);
        if (modelo.objetoVigilado != null)
            EstablecerObjetivo(Objetivo.RevisarObjeto);
        else
            EstablecerObjetivo(Objetivo.Patrullar);
    }

    public void RevisionTerminada()
    {
        if (modelo.objetoRobado)
            EstablecerObjetivo(Objetivo.Buscar);
        else
            EstablecerObjetivo(Objetivo.Patrullar);
    }
}
