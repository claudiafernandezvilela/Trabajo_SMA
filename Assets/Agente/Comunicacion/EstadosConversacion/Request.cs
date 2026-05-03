using UnityEngine;

// Request  —  rol: Gestor (agente que descubrió el objeto robado)
// OnEntrar: broadcast Request "asegurar_zona" pidiendo a otros que cubran más salidas.
// Ejecutar: espera Agree/Refuse; cierra la conversación al recibir todas las respuestas o deadline.
// OnMensaje: registra Agree/Refuse; un InformDone posterior confirma la finalización.
public class Request : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, Conversacion conv)
    {
        var r = (ConvRequest)conv;

        capa.Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.Request,
            capa.NombreAgente,
            null,
            "asegurar_zona",
            Vector3.zero,
            r.ConversationId));

        r.RespuestasEsperadas = 0;
        foreach (string agente in ProcesarMensajes.AgentesRegistrados())
            if (agente != capa.NombreAgente)
                r.RespuestasEsperadas++;

        r.Deadline = Time.time + 1.5f;
        Debug.Log($"[{capa.NombreAgente}] Request asegurar_zona → {r.RespuestasEsperadas} agentes conv:{r.ConversationId}");
    }

    public void Ejecutar(CapaComunicacion capa, Conversacion conv)
    {
        var r = (ConvRequest)conv;
        if (r.RespuestasRecibidas < r.RespuestasEsperadas && Time.time < r.Deadline) return;

        if (!r.AlguienAceptó)
            Debug.LogWarning($"[{capa.NombreAgente}] Ningún agente libre para asegurar zona adicional.");
        Transicion.A(capa, conv, new EstadoIdle());
    }

    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg)
    {
        var r = (ConvRequest)conv;

        switch (msg.performativa)
        {
            case Performativa.Agree:
                r.AlguienAceptó = true;
                r.RespuestasRecibidas++;
                Debug.Log($"[{capa.NombreAgente}] {msg.emisor} Agree asegurar_zona");
                break;

            case Performativa.Refuse:
                r.RespuestasRecibidas++;
                break;

            case Performativa.InformDone:
                Debug.Log($"[{capa.NombreAgente}] {msg.emisor} completó asegurar_zona (InformDone)");
                break;
        }
    }
}
