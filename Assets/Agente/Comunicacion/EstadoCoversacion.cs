using System.Collections.Generic;
using UnityEngine;

// ── Enum de fases (ya existía en el proyecto) ──────────────────────────────
public enum FaseContractNet
{
    Idle,
    CFP,          // Gestor emitió CFP / Contratistas lo recibieron y evalúan
    Esperando,    // Gestor espera propuestas hasta deadline
    Adjudicando,  // Gestor evalúa y decide / Contratistas esperan respuesta
    Ejecutando    // Contratista ganador ejecuta / Gestor espera inform o failure
}

// ── Interfaz ───────────────────────────────────────────────────────────────

/// Contrato que implementa cada estado de una conversación ContractNet.
/// Ejecutar() es el tick proactivo (se llama desde CapaComunicacion.Update).
/// OnMensaje() reacciona a un mensaje entrante ya filtrado por conversationId.
public interface IEstadoConversacion
{
    void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv);
    void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg);
}

// ══════════════════════════════════════════════════════════════════════════
// ESTADOS DEL GESTOR
// ══════════════════════════════════════════════════════════════════════════

/// El gestor acaba de crear la conversación.
/// Emite el CFP en broadcast y pasa a Esperando.
public class EstadoCFP : IEstadoConversacion
{
    private bool cfpEmitido = false;

    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        if (cfpEmitido) return;
        cfpEmitido = true;

        // El contenido del CFP lleva las tareas disponibles serializadas,
        // separadas por ';' para que los contratistas sepan qué se reparte.
        // La posición del ladrón va en msg.posicion (campo dedicado).
        var tareaStrings = new List<string>();
        foreach (var t in conv.TareasDisponibles)
            tareaStrings.Add(t.Serializar());
        string contenido = string.Join(";", tareaStrings);

        Vector3 posLadron = conv.TareasDisponibles.Count > 0
            ? conv.TareasDisponibles[0].posicionLadron
            : Vector3.zero;

        var cfp = new MensajeFIPA(
            Performativa.CFP,
            capa.NombreAgente,
            null,               // broadcast
            contenido,
            posLadron,
            conv.ConversationId);

        // Registra quiénes recibirán el CFP antes de enviarlo
        foreach (string agente in ProcesarMensajes.AgentesRegistrados())
            if (agente != capa.NombreAgente)
                conv.AgentesContactados.Add(agente);

        capa.Mensajes.EnviarMensaje(cfp);

        // Deadline: 2 segundos desde ahora
        conv.Deadline = Time.time + 2f;

        // Transición al estado de espera
        conv.SetEstado(new EstadoEsperando(), FaseContractNet.Esperando);
        Debug.Log($"[{capa.NombreAgente}] CFP emitido → conv:{conv.ConversationId}");
    }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}

/// El gestor espera propuestas. Acumula Propose/Refuse hasta el deadline.
public class EstadoEsperando : IEstadoConversacion
{
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        if (!conv.ListoParaAdjudicar) return;

        // Pasa a adjudicar en el mismo frame
        conv.SetEstado(new EstadoAdjudicando(), FaseContractNet.Adjudicando);
        conv.EstadoActual.Ejecutar(capa, conv);
    }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg)
    {
        if (msg.performativa == Performativa.Propose)
        {
            // contenido = puntuación (float serializado)
            if (float.TryParse(msg.contenido, out float puntuacion))
                conv.RegistrarPropuesta(msg.emisor, puntuacion);
            else
                Debug.LogWarning($"[{capa.NombreAgente}] Propose con puntuación inválida de {msg.emisor}");
        }
        else if (msg.performativa == Performativa.Refuse)
        {
            conv.RegistrarRechazo(msg.emisor);
        }
    }
}

/// El gestor evalúa las propuestas y adjudica tareas.
/// Ordena por puntuación ascendente (menor distancia = mejor)
/// y asigna una tarea por agente según la prioridad de TareasDisponibles.
public class EstadoAdjudicando : IEstadoConversacion
{
    private bool adjudicado = false;

    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        if (adjudicado) return;
        adjudicado = true;

        // Ordenar propuestas: menor puntuación (distancia) primero
        conv.Propuestas.Sort((a, b) => a.puntuacion.CompareTo(b.puntuacion));

        int tareaIdx = 0;
        var aceptados = new HashSet<string>();

        // Asignar una tarea a cada proponente en orden de mérito
        foreach (var propuesta in conv.Propuestas)
        {
            if (tareaIdx >= conv.TareasDisponibles.Count) break;

            TareaData tarea = conv.TareasDisponibles[tareaIdx];
            tareaIdx++;
            aceptados.Add(propuesta.emisor);

            var accept = new MensajeFIPA(
                Performativa.AcceptProposal,
                capa.NombreAgente,
                propuesta.emisor,
                tarea.Serializar(),          // contenido = tarea serializada
                tarea.posicionLadron,
                conv.ConversationId,
                replyWith: null,
                inReplyTo: conv.ConversationId);

            capa.Mensajes.EnviarMensaje(accept);
            Debug.Log($"[{capa.NombreAgente}] AcceptProposal → {propuesta.emisor} tarea:{tarea.tipo}");
        }

        // Rechazar al resto
        foreach (var propuesta in conv.Propuestas)
        {
            if (aceptados.Contains(propuesta.emisor)) continue;

            var reject = new MensajeFIPA(
                Performativa.RejectProposal,
                capa.NombreAgente,
                propuesta.emisor,
                "no_requerido",
                Vector3.zero,
                conv.ConversationId);

            capa.Mensajes.EnviarMensaje(reject);
            Debug.Log($"[{capa.NombreAgente}] RejectProposal → {propuesta.emisor}");
        }

        // La conversación del gestor termina aquí (espera inform opcionalmente)
        conv.SetEstado(new EstadoIdle(), FaseContractNet.Idle);
    }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}

// ══════════════════════════════════════════════════════════════════════════
// ESTADOS DEL CONTRATISTA
// ══════════════════════════════════════════════════════════════════════════

/// El contratista recibió un CFP. Evalúa si puede participar y propone.
/// No puede participar si el reactivo tiene un estado de prioridad alta activo.
public class EstadoPropose : IEstadoConversacion
{
    private bool propuestaEnviada = false;
    private readonly Vector3 posicionLadron;
    private readonly string conversationId;

    public EstadoPropose(Vector3 posicionLadron, string conversationId)
    {
        this.posicionLadron  = posicionLadron;
        this.conversationId = conversationId;
    }

    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        if (propuestaEnviada) return;
        propuestaEnviada = true;

        // Un contratista rechaza si ya está persiguiendo (estado de alta prioridad)
        if (capa.EstaOcupado())
        {
            var refuse = new MensajeFIPA(
                Performativa.Refuse,
                capa.NombreAgente,
                conv.GestorId,
                "ocupado",
                Vector3.zero,
                conv.ConversationId);
            capa.Mensajes.EnviarMensaje(refuse);
            conv.SetEstado(new EstadoIdle(), FaseContractNet.Idle);
            Debug.Log($"[{capa.NombreAgente}] Refuse → ocupado");
            return;
        }

        // Puntuación = distancia al ladrón (menor es mejor)
        float distancia = Vector3.Distance(capa.transform.position, posicionLadron);
        string puntuacionStr = distancia.ToString("F4");

        var propose = new MensajeFIPA(
            Performativa.Propose,
            capa.NombreAgente,
            conv.GestorId,
            puntuacionStr,
            capa.transform.position,
            conv.ConversationId);

        capa.Mensajes.EnviarMensaje(propose);
        conv.SetEstado(new EstadoEsperandoRespuesta(), FaseContractNet.Esperando);
        Debug.Log($"[{capa.NombreAgente}] Propose d={distancia:F2} → conv:{conv.ConversationId}");
    }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}

/// El contratista esperando saber si fue aceptado o rechazado.
public class EstadoEsperandoRespuesta : IEstadoConversacion
{
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg)
    {
        if (msg.performativa == Performativa.AcceptProposal)
        {
            TareaData tarea = TareaData.Deserializar(msg.contenido);
            if (tarea == null)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] AcceptProposal con tarea inválida");
                conv.SetEstado(new EstadoIdle(), FaseContractNet.Idle);
                return;
            }

            Debug.Log($"[{capa.NombreAgente}] Tarea asignada: {tarea.tipo}");
            conv.SetEstado(new EstadoEjecutando(tarea), FaseContractNet.Ejecutando);

            // Notifica al Cerebro para que cambie su comportamiento
            capa.OnTareaAsignada(tarea);
        }
        else if (msg.performativa == Performativa.RejectProposal)
        {
            Debug.Log($"[{capa.NombreAgente}] Propuesta rechazada, vuelvo a Idle");
            conv.SetEstado(new EstadoIdle(), FaseContractNet.Idle);
        }
    }
}

/// El contratista está ejecutando su tarea.
/// Cuando la completa (o falla) notifica al gestor y vuelve a Idle.
public class EstadoEjecutando : IEstadoConversacion
{
    private readonly TareaData tarea;

    public EstadoEjecutando(TareaData tarea)
    {
        this.tarea = tarea;
    }

    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }

    /// Llamado por CapaComunicacion cuando el Cerebro notifica que terminó la tarea.
    public void NotificarCompletada(CapaComunicacion capa, ConversacionContractNet conv)
    {
        var informDone = new MensajeFIPA(
            Performativa.InformDone,
            capa.NombreAgente,
            conv.GestorId,
            tarea.Serializar(),
            Vector3.zero,
            conv.ConversationId);

        capa.Mensajes.EnviarMensaje(informDone);
        conv.SetEstado(new EstadoIdle(), FaseContractNet.Idle);
        Debug.Log($"[{capa.NombreAgente}] InformDone tarea:{tarea.tipo}");
    }
}

// ══════════════════════════════════════════════════════════════════════════
// ESTADO NEUTRO
// ══════════════════════════════════════════════════════════════════════════

/// Estado por defecto. No hace nada. La conversación está cerrada o inactiva.
public class EstadoIdle : IEstadoConversacion
{
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }
    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}