using UnityEngine;

public interface IEstado
{
    void Ejecutar(Cerebro cerebro);
}

public class Patrulla : IEstado
{
    public void Ejecutar(Cerebro cerebro)
    {
        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
            cerebro.SiguientePunto();
    }
}

public class Perseguir : IEstado
{
    public void Ejecutar(Cerebro cerebro)
    {
        var modelo = cerebro.GetComponent<ModeloMundo>();
        if (modelo.player != null)
        {
            cerebro.agente.SetDestination(modelo.player.position);

            float distancia = Vector3.Distance(
                cerebro.transform.position,
                modelo.player.position);

            if (distancia <= cerebro.distanciaAtaque)
                cerebro.AtraparJugador();
        }
    }
}

public class Buscar : IEstado
{
    private bool destinoAsignado = false;
    private float timer = 0f;

    public void Ejecutar(Cerebro cerebro)
    {
        var modelo = cerebro.GetComponent<ModeloMundo>();

        if (!destinoAsignado)
        {
            cerebro.agente.SetDestination(modelo.ultimaPosicionJugador);
            destinoAsignado = true;
            return;
        }

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            timer += Time.deltaTime;
            if (timer >= cerebro.TiempoBusqueda)
                cerebro.GetComponent<CerebroDeliberativo>().BusquedaTerminada();
        }
    }
}

public class RevisarObjeto : IEstado
{
    private bool destinoAsignado = false;
    private AsegurarZona asegurarZona = null;

    public void Ejecutar(Cerebro cerebro)
    {
        var modelo = cerebro.GetComponent<ModeloMundo>();
        var deliberativo = cerebro.GetComponent<CerebroDeliberativo>();

        // Fase 1: ir al objeto
        if (asegurarZona == null)
        {
            if (!destinoAsignado)
            {
                cerebro.agente.SetDestination(modelo.posicionObjeto);
                destinoAsignado = true;
                return;
            }

            if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
            {
                if (modelo.objetoRobado)
                {
                    if (cerebro.PuntosAsegurarZona != null && cerebro.PuntosAsegurarZona.Length > 0)
                        asegurarZona = new AsegurarZona(cerebro.PuntosAsegurarZona);
                    else
                        deliberativo.RevisionTerminada();
                }
                else
                {
                    deliberativo.RevisionTerminada();
                }
            }
            return; // ← impide bajar al bloque de Fase 2 mientras asegurarZona es null
        }

        // Fase 2: recorrer puntos
        asegurarZona.Ejecutar(cerebro);

        if (asegurarZona.Terminado)
            deliberativo.RevisionTerminada();
    }
}

public class AsegurarZona
{
    public bool Terminado { get; private set; } = false;

    private Transform[] puntos;
    private int indiceActual = 0;
    private bool destinoAsignado = false;

    public AsegurarZona(Transform[] puntos)
    {
        this.puntos = puntos;
    }

    public void Ejecutar(Cerebro cerebro)
    {
        if (!destinoAsignado)
        {
            cerebro.agente.SetDestination(puntos[indiceActual].position);
            destinoAsignado = true;
            return;
        }

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            indiceActual++;

            if (indiceActual >= puntos.Length)
            {
                Terminado = true;
                return;
            }

            destinoAsignado = false;
        }
    }
}