using System.Collections.Generic;
using UnityEngine;

public enum RolContractNet { Gestor, Contratista }

/// Propuesta de un contratista para una tarea concreta.
internal class RegistroPropuesta
{
    public string emisor;
    public float  puntuacion;  // distancia al destino específico de la tarea

    public RegistroPropuesta(string emisor, float puntuacion)
    {
        this.emisor     = emisor;
        this.puntuacion = puntuacion;
    }
}

/// Estado de una conversación ContractNet en curso.
/// No es MonoBehaviour: vive dentro de CapaComunicacion.
public class ConversacionContractNet
{
    // ── identidad ──────────────────────────────────────────────────────────
    public string         ConversationId { get; }
    public RolContractNet Rol            { get; }

    // ── fase y estado activo ───────────────────────────────────────────────
    public FaseContractNet     Fase        { get; private set; }
    public IEstadoConversacion EstadoActual { get; private set; }

    // ── datos de dominio ───────────────────────────────────────────────────
    public string          GestorId          { get; set; }
    public List<TareaData> TareasDisponibles { get; } = new List<TareaData>();
    public List<string>    AgentesContactados { get; } = new List<string>();
    public float           Deadline          { get; set; }

    /// Si es false el gestor no entra en el pool de candidatos durante la adjudicación
    public bool GestorCompite { get; set; } = true;

    /// Propuestas agrupadas por índice de tarea.
    /// Clave: índice en TareasDisponibles. Valor: lista de propuestas para esa tarea.
    internal Dictionary<int, List<RegistroPropuesta>> PropuestasPorTarea { get; }
        = new Dictionary<int, List<RegistroPropuesta>>();

    /// Total de propuestas recibidas (una por tarea por contratista).
    /// El gestor espera AgentesContactados.Count * TareasDisponibles.Count propuestas
    /// o hasta que expire el deadline.
    public int TotalPropuestasRecibidas { get; private set; }

    private int TotalPropuestasEsperadas =>
        AgentesContactados.Count * TareasDisponibles.Count;

    // ── constructor ────────────────────────────────────────────────────────
    public ConversacionContractNet(string conversationId, RolContractNet rol)
    {
        ConversationId = conversationId;
        Rol            = rol;
        Fase           = FaseContractNet.Idle;
    }

    // ── transición ─────────────────────────────────────────────────────────
    public void SetEstado(IEstadoConversacion nuevoEstado, FaseContractNet nuevaFase)
    {
        Fase         = nuevaFase;
        EstadoActual = nuevoEstado;
    }

    // ── helpers para el gestor ─────────────────────────────────────────────

    /// Registra la propuesta de un contratista para una tarea concreta.
    /// tareaIdx es el índice en TareasDisponibles.
    public void RegistrarPropuesta(string emisor, int tareaIdx, float puntuacion)
    {
        if (!PropuestasPorTarea.ContainsKey(tareaIdx))
            PropuestasPorTarea[tareaIdx] = new List<RegistroPropuesta>();

        PropuestasPorTarea[tareaIdx].Add(new RegistroPropuesta(emisor, puntuacion));
        TotalPropuestasRecibidas++;

        //Debug.Log($"[Conv {ConversationId}] Propuesta: {emisor} tarea[{tareaIdx}]" +
                  //$"={TareasDisponibles[tareaIdx].tipo} d={puntuacion:F2}");
    }

    public void RegistrarRechazo(string emisor)
    {
        // Un rechazo cuenta como una propuesta por cada tarea disponible
        TotalPropuestasRecibidas += TareasDisponibles.Count;
        Debug.Log($"[Conv {ConversationId}] {emisor} rechazó participar.");
    }

    /// True cuando se han recibido todas las propuestas esperadas o expiró el deadline.
    public bool ListoParaAdjudicar =>
        TotalPropuestasRecibidas >= TotalPropuestasEsperadas || Time.time >= Deadline;
}