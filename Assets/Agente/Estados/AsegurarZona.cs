using UnityEngine;

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