using UnityEngine;

public class Buscar : IEstado
{
    private float timer                  = 0f;
    private bool  queryIniciada          = false;
    private bool  llegado                = false;
    private float tiempoUltimaPrediccion = -1f;
    private const float IntervaloPrediccion = 1f;

    public void Ejecutar(Cerebro cerebro)
    {
        // Actualizar destino mientras el agente no ha llegado y el QueryIf no está en curso.
        if (!llegado && !queryIniciada
            && Time.time - tiempoUltimaPrediccion >= IntervaloPrediccion)
        {
            tiempoUltimaPrediccion = Time.time;
            cerebro.agente.SetDestination(cerebro.ObtenerPosicionPredichaLadron());
        }

        if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
        {
            llegado = true;
            cerebro.transform.Rotate(0f, 60f * Time.deltaTime, 0f);
            timer += Time.deltaTime;

            if (timer >= cerebro.TiempoBusqueda && !queryIniciada)
            {
                queryIniciada = true;
                cerebro.IniciarQuery();
            }
        }
    }
}
