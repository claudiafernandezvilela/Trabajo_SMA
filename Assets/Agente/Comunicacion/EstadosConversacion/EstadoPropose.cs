using System.Collections.Generic;
using UnityEngine;
// OnEntrar: envía un Propose por cada tarea con la distancia al destino
//           específico de esa tarea, o un Refuse si está ocupado.
// Ejecutar: vacío, espera la respuesta del gestor.
// OnMensaje: Accept → Ejecutando / Reject → Idle.
public class EstadoPropose : IEstadoConversacion
{
    private readonly Vector3 posicionLadron;
 
    public EstadoPropose(Vector3 posicionLadron)
    {
        this.posicionLadron = posicionLadron;
    }
 
    public void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        if (capa.EstaOcupado())
        {
            capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                Performativa.Refuse,
                capa.NombreAgente,
                conv.GestorId,
                "ocupado",
                Vector3.zero,
                conv.ConversationId));
 
            Debug.Log($"[{capa.NombreAgente}] Refuse → ocupado");
            Transicion.A(capa, conv, new EstadoIdle(), FaseContractNet.Idle);
            return;
        }
 
        // Enviar un Propose por cada tarea con la distancia al destino específico
        for (int i = 0; i < conv.TareasDisponibles.Count; i++)
        {
            TareaData tarea = conv.TareasDisponibles[i];
 
            float distancia = Vector3.Distance(capa.transform.position, tarea.DestinoEjecucion);
 
            // Formato contenido: "tareaIdx|puntuacion"
            string contenido = $"{i}|{distancia:F4}";
 
            capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                Performativa.Propose,
                capa.NombreAgente,
                conv.GestorId,
                contenido,
                capa.transform.position,
                conv.ConversationId));
 
            Debug.Log($"[{capa.NombreAgente}] Propose tarea[{i}]={tarea.tipo}" +
                      $" d={distancia:F2} destino={tarea.DestinoEjecucion} → conv:{conv.ConversationId}");
        }
 
        // Espera una respuesta del gestor por cada tarea propuesta
        respuestasPendientes = conv.TareasDisponibles.Count;
    }
 
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }
 
    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg)
    {
        if (msg.performativa == Performativa.AcceptProposal)
        {
            TareaData tarea = TareaData.Deserializar(msg.contenido);
            if (tarea == null)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] AcceptProposal con tarea inválida");
                Transicion.A(capa, conv, new EstadoIdle(), FaseContractNet.Idle);
                return;
            }
            Debug.Log($"[{capa.NombreAgente}] AcceptProposal → tarea:{tarea.tipo}" +
                      $" destino:{tarea.DestinoEjecucion}");
            // El Accept tiene prioridad: transita a Ejecutando aunque queden Rejects pendientes.
            // Los Rejects que lleguen después se ignorarán porque la conversación estará en Ejecutando.
            Transicion.A(capa, conv, new EstadoEjecutando(tarea), FaseContractNet.Ejecutando);
        }
        else if (msg.performativa == Performativa.RejectProposal)
        {
            // Solo transitar a Idle si no hemos recibido ningún Accept
            // (la conversación sigue en fase Propose).
            if (conv.Fase == FaseContractNet.Propose)
            {
                respuestasPendientes--;
                Debug.Log($"[{capa.NombreAgente}] RejectProposal ({respuestasPendientes} pendientes)");
                if (respuestasPendientes <= 0)
                    Transicion.A(capa, conv, new EstadoIdle(), FaseContractNet.Idle);
            }
        }
    }
 
    // Cuenta cuántas respuestas (Accept o Reject) espera el contratista, una por cada tarea para la que propuso.
    private int respuestasPendientes;
}