using System.Collections.Generic;
using UnityEngine;
// Adjudicando  —  rol: Gestor
// OnEntrar: para cada tarea en orden de prioridad, elige al contratista
//           con menor distancia que no haya sido asignado ya.
//           Envía Accept al ganador y Reject al resto. Cierra la conversación.
public class EstadoAdjudicando : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        var asignados = new HashSet<string>();
 
        // Iterar tareas en orden de prioridad: BloquearSalida1, BloquearSalida2, Buscar
        for (int i = 0; i < conv.TareasDisponibles.Count; i++)
        {
            TareaData tarea = conv.TareasDisponibles[i];
 
            if (!conv.PropuestasPorTarea.TryGetValue(i, out var propuestas)
                || propuestas.Count == 0)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Sin propuestas para tarea[{i}]={tarea.tipo}");
                continue;
            }
 
            // Ordenar por distancia ascendente y elegir el primero no asignado
            propuestas.Sort((a, b) => a.puntuacion.CompareTo(b.puntuacion));
 
            string ganador = null;
            foreach (var p in propuestas)
            {
                if (!asignados.Contains(p.emisor))
                {
                    ganador = p.emisor;
                    break;
                }
            }
 
            if (ganador == null)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] No hay candidato libre para tarea[{i}]={tarea.tipo}");
                continue;
            }
 
            asignados.Add(ganador);
 
            // Enviar Accept al ganador
            capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                Performativa.AcceptProposal,
                capa.NombreAgente,
                ganador,
                tarea.Serializar(),
                tarea.posicionLadron,
                conv.ConversationId));
 
            Debug.Log($"[{capa.NombreAgente}] Accept → {ganador} tarea:{tarea.tipo}");
 
            // Enviar Reject al resto que propusieron para esta tarea
            foreach (var p in propuestas)
            {
                if (p.emisor == ganador) continue;
                if (asignados.Contains(p.emisor)) continue;
 
                capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                    Performativa.RejectProposal,
                    capa.NombreAgente,
                    p.emisor,
                    "no_requerido",
                    Vector3.zero,
                    conv.ConversationId));
 
                Debug.Log($"[{capa.NombreAgente}] Reject → {p.emisor} tarea:{tarea.tipo}");
            }
        }
 
        Transicion.A(capa, conv, new EstadoIdle(), FaseContractNet.Idle);
    }
 
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }
    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}