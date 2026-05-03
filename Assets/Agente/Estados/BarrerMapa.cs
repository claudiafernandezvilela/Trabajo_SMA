public class BarrerMapa : IEstado
{
    private int indiceActual = 0;

    public void Ejecutar(Cerebro cerebro)
    {
        // Salir si ya terminamos pero el estado aún no cambió
        if (indiceActual >= cerebro.PuntosBarrido.Length) return;

        cerebro.agente.SetDestination(cerebro.PuntosBarrido[indiceActual].position);

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            indiceActual++;
            if (indiceActual >= cerebro.PuntosBarrido.Length)
                cerebro.NotificarEvento(Evento.BarridoTerminado);
        }
    }
}