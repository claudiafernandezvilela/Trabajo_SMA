using UnityEngine;
public class Perseguir : IEstado
{
    public void Ejecutar(Cerebro cerebro)
    {
        cerebro.agente.SetDestination(cerebro.Modelo.player.position);
    }
}
