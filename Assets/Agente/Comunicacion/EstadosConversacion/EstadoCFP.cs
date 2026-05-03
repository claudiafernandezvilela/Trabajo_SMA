using System.Collections.Generic;
using UnityEngine;
// CFP  —  rol: Gestor
// OnEntrar: emite el CFP broadcast una sola vez y fija el deadline.
// Ejecutar: comprueba si ya llegaron todas las respuestas para transitar a Adjudicando.
// OnMensaje: acumula Propose / Refuse.
public class EstadoCFP : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, Conversacion conv)
    {
        var cnet = (ConvCNet)conv;
        var partes = new List<string>();
        foreach (var t in cnet.TareasDisponibles)
            partes.Add(t.Serializar());
 
        Vector3 posLadron = cnet.TareasDisponibles.Count > 0
            ? cnet.TareasDisponibles[0].posicionLadron
            : Vector3.zero;
 
        var cfp = new MensajeFIPA(
            Performativa.CFP,
            capa.NombreAgente,
            null,
            string.Join(";", partes),
            posLadron,
            cnet.ConversationId);
 
        foreach (string agente in ProcesarMensajes.AgentesRegistrados())
            if (agente != capa.NombreAgente)
                cnet.AgentesContactados.Add(agente);
 
        capa.Mensajes.EnviarMensaje(cfp);
        cnet.Deadline = Time.time + 2f;
 
        Debug.Log($"[{capa.NombreAgente}] CFP emitido {cnet.TareasDisponibles.Count} tareas" +
                  $" → conv:{cnet.ConversationId}");
    }
 
    public void Ejecutar(CapaComunicacion capa, Conversacion conv)
    {
        var cnet = (ConvCNet)conv;
        if (!cnet.ListoParaAdjudicar) return;
                Transicion.A(capa, cnet, new EstadoAdjudicando(), FaseContractNet.Adjudicando);
    }
 
    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg)
    {
        var cnet = (ConvCNet)conv;
        if (msg.performativa == Performativa.Propose)
        {
            // Formato contenido: "tareaIdx|puntuacion"
            string[] partes = msg.contenido.Split('|');
            if (partes.Length == 2
                && int.TryParse(partes[0], out int tareaIdx)
                && float.TryParse(partes[1], out float puntuacion))
            {
                cnet.RegistrarPropuesta(msg.emisor, tareaIdx, puntuacion);
            }
            else
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Propose malformado de {msg.emisor}: {msg.contenido}");
            }
        }
        else if (msg.performativa == Performativa.Refuse)
        {
            cnet.RegistrarRechazo(msg.emisor);
        }
    }
}