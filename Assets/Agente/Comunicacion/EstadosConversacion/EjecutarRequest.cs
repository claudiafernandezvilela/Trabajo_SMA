using System.Collections.Generic;
using UnityEngine;

// Ejecutando  —  rol: Contratista (agente que aceptó un Request de asegurar zona)
// OnEntrar: registra que la tarea ha comenzado.
// Ejecutar: vacío; el Cerebro gestiona la ejecución de AsegurarZona.
// NotificarCompletada: llamado por CapaComunicacion cuando AsegurarZona termina;
//                      envía InformDone al gestor y cierra la conversación.
public class EjecutarRequest : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, Conversacion conv)
    {
        Debug.Log($"[{capa.NombreAgente}] Ejecutando Request asegurar_zona → gestor:{conv.InterlocutorId}");
    }

    public void Ejecutar(CapaComunicacion capa, Conversacion conv) { }

    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg) { }

    public void NotificarCompletada(CapaComunicacion capa, Conversacion conv)
    {
        capa.Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.InformDone,
            capa.NombreAgente,
            conv.InterlocutorId,
            "asegurar_zona",
            Vector3.zero,
            conv.ConversationId));

        Debug.Log($"[{capa.NombreAgente}] InformDone asegurar_zona → {conv.InterlocutorId}");
        Transicion.A(capa, conv, new EstadoIdle());
    }
}
