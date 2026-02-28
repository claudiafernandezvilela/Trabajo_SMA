using UnityEngine;

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
        if (cerebro.player != null)
        {
            cerebro.agente.SetDestination(cerebro.player.position);

            float distancia = Vector3.Distance(
                cerebro.transform.position,
                cerebro.player.position
            );

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
        if (!destinoAsignado)
        {
            cerebro.agente.SetDestination(cerebro.UltimaPosicion);
            destinoAsignado = true;
            return; // esperar al siguiente frame
        }

        if (!cerebro.agente.pathPending &&
            cerebro.agente.remainingDistance < 0.5f)
        {
            timer += Time.deltaTime;

            if (timer >= cerebro.TiempoBusqueda)
            {
                cerebro.BusquedaTerminada();
            }
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
            cerebro.agente.SetDestination(cerebro.posicionObjeto);
            destinoAsignado = true;
            return; // esperar al siguiente frame
        }

        if (!cerebro.agente.pathPending &&
            cerebro.agente.remainingDistance < 0.5f)
        {
            cerebro.RevisionTerminada();
        }
    }
}


