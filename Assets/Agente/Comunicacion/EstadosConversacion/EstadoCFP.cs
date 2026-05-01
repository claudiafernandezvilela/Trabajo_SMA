using System.Collections.Generic;
using UnityEngine;
// CFP  —  rol: Gestor
// OnEntrar: emite el CFP broadcast una sola vez y fija el deadline.
// Ejecutar: comprueba si ya llegaron todas las respuestas para transitar a Adjudicando.
// OnMensaje: acumula Propose / Refuse.
public class EstadoCFP : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        var partes = new List<string>();
        foreach (var t in conv.TareasDisponibles)
            partes.Add(t.Serializar());
 
        Vector3 posLadron = conv.TareasDisponibles.Count > 0
            ? conv.TareasDisponibles[0].posicionLadron
            : Vector3.zero;
 
        var cfp = new MensajeFIPA(
            Performativa.CFP,
            capa.NombreAgente,
            null,
            string.Join(";", partes),
            posLadron,
            conv.ConversationId);
 
        foreach (string agente in ProcesarMensajes.AgentesRegistrados())
            if (agente != capa.NombreAgente)
                conv.AgentesContactados.Add(agente);
 
        capa.Mensajes.EnviarMensaje(cfp);
        conv.Deadline = Time.time + 2f;
 
        Debug.Log($"[{capa.NombreAgente}] CFP emitido {conv.TareasDisponibles.Count} tareas" +
                  $" → conv:{conv.ConversationId}");
    }
 
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        if (!conv.ListoParaAdjudicar) return;
        Transicion.A(capa, conv, new EstadoAdjudicando(), FaseContractNet.Adjudicando);
    }
 
    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg)
    {
        if (msg.performativa == Performativa.Propose)
        {
            // Formato contenido: "tareaIdx|puntuacion"
            string[] partes = msg.contenido.Split('|');
            if (partes.Length == 2
                && int.TryParse(partes[0], out int tareaIdx)
                && float.TryParse(partes[1], out float puntuacion))
            {
                conv.RegistrarPropuesta(msg.emisor, tareaIdx, puntuacion);
            }
            else
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Propose malformado de {msg.emisor}: {msg.contenido}");
            }
        }
        else if (msg.performativa == Performativa.Refuse)
        {
            conv.RegistrarRechazo(msg.emisor);
        }
    }
}