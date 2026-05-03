using System.Collections.Generic;
using UnityEngine;
// Ejecutando  —  rol: Contratista
// OnEntrar: notifica al Cerebro que debe ejecutar la tarea asignada.
// Ejecutar: vacío, el Cerebro gestiona la ejecución.
// OnMensaje: vacío.

public class EstadoEjecutando : IEstadoConversacion
{
    private readonly TareaData tarea;

    public EstadoEjecutando(TareaData tarea)
    {
        this.tarea = tarea;
    }

    public void OnEntrar(CapaComunicacion capa, Conversacion conv)
    {
        Debug.Log($"[{capa.NombreAgente}] Ejecutando tarea:{tarea.tipo}");
        capa.OnTareaAsignada(tarea);
    }

    public void Ejecutar(CapaComunicacion capa, Conversacion conv) { }
    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg) { }

    /// Llamado desde CapaComunicacion cuando el Cerebro notifica que terminó la tarea.
    public void NotificarCompletada(CapaComunicacion capa, ConvCNet cnet)
    {
        capa.Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.InformDone,
            capa.NombreAgente,
            cnet.GestorId,
            tarea.Serializar(),
            Vector3.zero,
            cnet.ConversationId));

        Debug.Log($"[{capa.NombreAgente}] InformDone tarea:{tarea.tipo}");
        Transicion.A(capa, cnet, new EstadoIdle(), FaseContractNet.Idle);
    }
}