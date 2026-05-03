using System.Collections.Generic;
using UnityEngine;

public enum RolContractNet { Gestor, Contratista }

/// Proposal from a contractor for a specific task.
internal class RegistroPropuesta
{
    public string emisor;
    public float  puntuacion;

    public RegistroPropuesta(string emisor, float puntuacion)
    {
        this.emisor     = emisor;
        this.puntuacion = puntuacion;
    }
}

/// State of an active ContractNet conversation.
/// Not a MonoBehaviour — lives inside CapaComunicacion.
public class ConvCNet : Conversacion
{
    // ── identidad ──────────────────────────────────────────────────────────
    public RolContractNet  Rol  { get; }
    public FaseContractNet Fase { get; private set; }

    /// Alias for InterlocutorId, kept for readability in CNet-specific code.
    public string GestorId
    {
        get => InterlocutorId;
        set => InterlocutorId = value;
    }

    // ── datos de dominio ───────────────────────────────────────────────────
    public List<TareaData> TareasDisponibles  { get; } = new List<TareaData>();
    public List<string>    AgentesContactados { get; } = new List<string>();
    public float           Deadline           { get; set; }
    public bool            GestorCompite      { get; set; } = true;

    internal Dictionary<int, List<RegistroPropuesta>> PropuestasPorTarea { get; }
        = new Dictionary<int, List<RegistroPropuesta>>();

    public int TotalPropuestasRecibidas { get; private set; }

    private int TotalPropuestasEsperadas =>
        AgentesContactados.Count * TareasDisponibles.Count;

    // ── constructor ────────────────────────────────────────────────────────
    public ConvCNet(string conversationId, RolContractNet rol)
        : base(conversationId)
    {
        Rol  = rol;
        Fase = FaseContractNet.Idle;
    }

    // ── transición ─────────────────────────────────────────────────────────
    /// CNet overload: sets both the state and the protocol phase.
    public void SetEstado(IEstadoConversacion nuevoEstado, FaseContractNet nuevaFase)
    {
        Fase = nuevaFase;
        base.SetEstado(nuevoEstado);
    }

    // ── BloqueaAgente ──────────────────────────────────────────────────────
    /// Blocks the agent while adjudicating (waiting for InformDone) or executing.
    public override bool BloqueaAgente() =>
        Fase == FaseContractNet.Ejecutando || Fase == FaseContractNet.Adjudicando;

    // ── helpers para el gestor ─────────────────────────────────────────────
    public void RegistrarPropuesta(string emisor, int tareaIdx, float puntuacion)
    {
        if (!PropuestasPorTarea.ContainsKey(tareaIdx))
            PropuestasPorTarea[tareaIdx] = new List<RegistroPropuesta>();

        PropuestasPorTarea[tareaIdx].Add(new RegistroPropuesta(emisor, puntuacion));
        TotalPropuestasRecibidas++;
    }

    public void RegistrarRechazo(string emisor)
    {
        TotalPropuestasRecibidas += TareasDisponibles.Count;
        Debug.Log($"[Conv {ConversationId}] {emisor} rechazó participar.");
    }

    public bool ListoParaAdjudicar =>
        TotalPropuestasRecibidas >= TotalPropuestasEsperadas || Time.time >= Deadline;
}
