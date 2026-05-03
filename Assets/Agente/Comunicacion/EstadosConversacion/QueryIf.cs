using UnityEngine;

// QueryIf  —  rol: Gestor (agente cuya búsqueda expiró sin encontrar al ladrón)
// OnEntrar: broadcast QueryIf "ladron_cercano" a todos los demás agentes.
// Ejecutar: espera InformIf de todos o hasta deadline y notifica el resultado al Cerebro.
// OnMensaje: acumula InformIf; guarda la posición más reciente recibida con "true".
public class QueryIf : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, Conversacion conv)
    {
        var q = (ConvQueryIf)conv;

        capa.Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.QueryIf,
            capa.NombreAgente,
            null,
            "ladron_cercano",
            Vector3.zero,
            q.ConversationId));

        q.RespuestasEsperadas = 0;
        foreach (string agente in ProcesarMensajes.AgentesRegistrados())
            if (agente != capa.NombreAgente)
                q.RespuestasEsperadas++;

        q.Deadline = Time.time + 1f;
        Debug.Log($"[{capa.NombreAgente}] QueryIf ladron_cercano → {q.RespuestasEsperadas} agentes conv:{q.ConversationId}");
    }

    public void Ejecutar(CapaComunicacion capa, Conversacion conv)
    {
        var q = (ConvQueryIf)conv;
        if (q.RespuestasRecibidas < q.RespuestasEsperadas && Time.time < q.Deadline) return;

        bool encontrado = q.MejorPosicion != Vector3.zero;
        Debug.Log($"[{capa.NombreAgente}] QueryIf resultado: encontrado={encontrado} pos={q.MejorPosicion}");
        capa.OnResultadoQueryBusqueda(encontrado, q.MejorPosicion);
        Transicion.A(capa, conv, new EstadoIdle());
    }

    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg)
    {
        if (msg.performativa != Performativa.InformIf) return;

        var q = (ConvQueryIf)conv;
        q.RespuestasRecibidas++;
        if (msg.contenido == "true" && msg.posicion != Vector3.zero && msg.timestamp > q.MejorTimestamp)
        {
            q.MejorTimestamp = msg.timestamp;
            q.MejorPosicion  = msg.posicion;
        }
    }
}
