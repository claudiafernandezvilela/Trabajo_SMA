using UnityEngine;

public interface IEstado
{
    void Ejecutar(Cerebro cerebro);
}

public class Patrulla : IEstado
{
    private int indiceActual = 0;
    public void Ejecutar(Cerebro cerebro)
    {
        // El camino esta calculado y el agente ha llegado a su destino
        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            indiceActual = (indiceActual + 1) % cerebro.PuntosPatrullas.Length;
            cerebro.agente.SetDestination(cerebro.PuntosPatrullas[indiceActual].position);
        }
    }
}

public class Perseguir : IEstado
{
    public void Ejecutar(Cerebro cerebro)
    {
        cerebro.agente.SetDestination(cerebro.Modelo.player.position);
    }
}

public class Buscar : IEstado
{
    private float timer = 0f;

    public void Ejecutar(Cerebro cerebro)
    {
        cerebro.agente.SetDestination(cerebro.Modelo.ultimaPosicionJugador);

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            timer += Time.deltaTime;
            if (timer >= cerebro.TiempoBusqueda)
                cerebro.NotificarEvento(Evento.BusquedaTerminada);
        }
    }
}

public class RevisarObjeto : IEstado
{
    private bool destinoAsignado = false;
    public void Ejecutar(Cerebro cerebro)
    {
        if (!destinoAsignado)
        {
            cerebro.agente.SetDestination(cerebro.Modelo.posicionObjeto);
            destinoAsignado = true;
            return; // espera al siguiente frame
        }
        // El camino esta calculado y el agente ha llegado a su destino
        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
            cerebro.NotificarEvento(Evento.RevisionTerminada);
    }
}

public class AsegurarZona : IEstado
{
    private int  indiceActual    = 0;
    public void Ejecutar(Cerebro cerebro)
    {
        cerebro.agente.SetDestination(cerebro.PuntosAsegurarZona[indiceActual].position);
        // El camino esta calculado y el agente ha llegado a su destino
        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            indiceActual++;
            if (indiceActual >= cerebro.PuntosAsegurarZona.Length)
                cerebro.NotificarEvento(Evento.AsegurarZonaTerminada);
        }
    }
}