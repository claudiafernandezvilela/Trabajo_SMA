using UnityEngine;

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
