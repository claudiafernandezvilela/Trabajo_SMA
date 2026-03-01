using UnityEngine;
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