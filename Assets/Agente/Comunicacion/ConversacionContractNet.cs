using System.Collections.Generic;
using UnityEngine;

/// Rol que tiene este agente en una conversación ContractNet concreta.
/// El rol se fija al crear la conversación y no cambia.
public enum RolContractNet { Gestor, Contratista }

/// Registro de la propuesta de un contratista.
internal class RegistroPropuesta
{
    public string emisor;
    public float  puntuacion;   // menor = mejor (distancia al ladrón)
    public bool   rechazado;    // true si ya se le envió RejectProposal

    public RegistroPropuesta(string emisor, float puntuacion)
    {
        this.emisor      = emisor;
        this.puntuacion  = puntuacion;
        this.rechazado   = false;
    }
}

/// Estado de una conversación ContractNet en curso.
/// No es MonoBehaviour: vive dentro de CapaComunicacion.
/// Toda la lógica de transición está en los IEstadoConversacion;
/// esta clase solo almacena los datos de la conversación.
public class ConversacionContractNet
{
    // ── identidad ──────────────────────────────────────────────────────────
    public string          ConversationId  { get; }
    public RolContractNet  Rol             { get; }

    // ── fase ───────────────────────────────────────────────────────────────
    public FaseContractNet Fase            { get; private set; }

    // ── estado activo (máquina de estados de la conversación) ──────────────
    public IEstadoConversacion EstadoActual { get; private set; }

    // ── datos de dominio (accesibles por los estados) ──────────────────────

    /// Tareas disponibles en este ContractNet, ordenadas por prioridad.
    /// Rellenas por el gestor al crear la conversación.
    public List<TareaData> TareasDisponibles { get; } = new List<TareaData>();

    /// Propuestas recibidas (solo el gestor las acumula).
    internal List<RegistroPropuesta> Propuestas { get; } = new List<RegistroPropuesta>();

    /// Nombre del agente que inició el protocolo (el gestor).
    /// Los contratistas lo usan para saber a quién enviar la propuesta.
    public string GestorId { get; set; }

    /// Agentes a los que se ha enviado el CFP (el gestor lleva la cuenta
    /// para saber cuándo ha recibido todas las respuestas).
    public List<string> AgentesContactados { get; } = new List<string>();

    /// Número de agentes que ya han respondido (Propose o Refuse).
    public int Respuestas { get; private set; }

    /// Deadline: tiempo Unity a partir del cual el gestor adjudica
    /// aunque no haya recibido todas las respuestas.
    public float Deadline { get; set; }

    // ── constructor ────────────────────────────────────────────────────────
    public ConversacionContractNet(string conversationId, RolContractNet rol)
    {
        ConversationId = conversationId;
        Rol            = rol;
        Fase           = FaseContractNet.Idle;
    }

    // ── transición de estado ───────────────────────────────────────────────
    /// Cambia el estado activo de la conversación y actualiza la fase.
    public void SetEstado(IEstadoConversacion nuevoEstado, FaseContractNet nuevaFase)
    {
        Fase         = nuevaFase;
        EstadoActual = nuevoEstado;
    }

    // ── helpers para el gestor ─────────────────────────────────────────────

    /// Registra una propuesta de un contratista.
    public void RegistrarPropuesta(string emisor, float puntuacion)
    {
        Propuestas.Add(new RegistroPropuesta(emisor, puntuacion));
        Respuestas++;
    }

    /// Marca que un contratista ha rechazado participar.
    public void RegistrarRechazo(string emisor)
    {
        Respuestas++;
        Debug.Log($"[Conv {ConversationId}] {emisor} rechazó participar.");
    }

    /// True cuando el gestor ya ha recibido respuesta de todos los contactados
    /// O cuando ha expirado el deadline.
    public bool ListoParaAdjudicar =>
        Respuestas >= AgentesContactados.Count || Time.time >= Deadline;
}