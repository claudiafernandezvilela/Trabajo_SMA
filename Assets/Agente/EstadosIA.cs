using UnityEngine;

public class Patrulla : IEstado
{
    public void Ejecutar(Cerebro cerebro)
    {
        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
            cerebro.SiguientePunto();

        if (cerebro.JugadorVisible)
            cerebro.CambiarEstado(new Perseguir());
    }
}

public class Perseguir : IEstado
{
    public void Ejecutar(Cerebro cerebro)
    {
        if (cerebro.player != null)
            cerebro.agente.SetDestination(cerebro.player.position);

        if (!cerebro.JugadorVisible)
        {
            cerebro.UltimaPosicion = cerebro.player.position;
            cerebro.CambiarEstado(new Buscar());
        }

        // Comprobar si alcanzó al jugador
        if (cerebro.player != null)
        {
            float distancia = Vector3.Distance(cerebro.transform.position, cerebro.player.position);
            if (distancia <= cerebro.distanciaAtaque)
            {
                cerebro.AtraparJugador();
            }
        }
    }
}

public class Buscar : IEstado
{
    private bool llegue = false;
    private float timer = 0f;

    public void Ejecutar(Cerebro cerebro)
    {
        if (!llegue)
        {
            cerebro.agente.SetDestination(cerebro.UltimaPosicion);
            llegue = true;
        }

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            timer += UnityEngine.Time.deltaTime;
            if (timer >= cerebro.TiempoBusqueda)
                // Si tiene objeto vigilado, ir a revisarlo 
                // (ns si cambiarlo pq realmente todos los agentes tienen objeto asigando)
                if (cerebro.objetoVigilado != null)
                    cerebro.CambiarEstado(new RevisarObjeto());
                else
                    cerebro.CambiarEstado(new Patrulla());
        }

        if (cerebro.JugadorVisible)
            cerebro.CambiarEstado(new Perseguir());
    }
}

public class RevisarObjeto : IEstado
{
    private bool llegue = false;

    public void Ejecutar(Cerebro cerebro)
    {
        if (!llegue)
        {
            cerebro.agente.SetDestination(cerebro.posicionObjeto);
            llegue = true;
        }

        if (cerebro.JugadorVisible)
        {
            cerebro.CambiarEstado(new Perseguir());
            return;
        }

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            if (Objeto.fueRecogido)
            {
                // El objeto desapareció, seguir buscando al ladrón
                cerebro.CambiarEstado(new Buscar());
            }
            else
            {
                // El objeto sigue ahí, volver a patrullar
                cerebro.CambiarEstado(new Patrulla());
            }
        }
    }
}

