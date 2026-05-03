using UnityEngine;

// El agente se desplaza a la posición de una puerta y se queda bloqueándola.
// Cuando llega, vuelve FaseConversacion a Idle para poder aceptar nuevas tareas.
// El CerebroReactivo puede interrumpir este estado si el ladrón entra en visión.
public class BloquearSalida : IEstado
{
    private readonly Vector3 destino;
    private bool llegado = false;
    private float tiempoLlegada = -1f;
    private bool expiracionNotificada = false;
    private const float TiempoEspera = 10f;
    public BloquearSalida(Vector3 destino)
    {
        this.destino = destino;
    }

    public void Ejecutar(Cerebro cerebro)
    {
        if (!llegado)
        {
            cerebro.agente.SetDestination(destino);
            if (!cerebro.agente.pathPending && cerebro.agente.remainingDistance < 0.5f)
            {
                llegado = true;
                tiempoLlegada = Time.time;
                cerebro.agente.ResetPath();
                Debug.Log($"[BloquearSalida] {cerebro.name} bloqueando puerta en {destino}.");
                cerebro.NotificarEvento(Evento.BloquearSalidaTerminado);
            }
            return;
        }

        if (!expiracionNotificada && Time.time - tiempoLlegada >= TiempoEspera)        {
                        expiracionNotificada = true;
            cerebro.NotificarEvento(Evento.BloquearSalidaExpiro);
        }
    }
}