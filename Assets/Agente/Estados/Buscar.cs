using UnityEngine;

public class Buscar : IEstado
{
    private float timer = 0f;

    public void Ejecutar(Cerebro cerebro)
    {
        cerebro.agente.SetDestination(cerebro.Modelo.ultimaPosicionJugador);

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            cerebro.transform.Rotate(0f, 60f * Time.deltaTime, 0f);
            timer += Time.deltaTime;
            if (timer >= cerebro.TiempoBusqueda)
                cerebro.NotificarEvento(Evento.BusquedaTerminada);
        }
    }
}

