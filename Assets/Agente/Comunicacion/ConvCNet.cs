using System.Collections.Generic;
using UnityEngine;

public enum RolContractNet { Gestor, Contratista }

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

public class ConvCNet : Conversacion
{
    public RolContractNet  Rol  { get; }
    public FaseContractNet Fase { get; private set; }
    public string GestorId
    {
        get => InterlocutorId;
        set => InterlocutorId = value;
    }
    public List<TareaData> TareasDisponibles  { get; } = new List<TareaData>();
    public List<string>    AgentesContactados { get; } = new List<string>();
    public float           Deadline           { get; set; }
    public bool            GestorCompite      { get; set; } = true;

    internal Dictionary<int, List<RegistroPropuesta>> PropuestasPorTarea { get; }
        = new Dictionary<int, List<RegistroPropuesta>>();

    public int TotalPropuestasRecibidas { get; private set; }

    private int TotalPropuestasEsperadas =>
        AgentesContactados.Count * TareasDisponibles.Count;

    public ConvCNet(string conversationId, RolContractNet rol)
        : base(conversationId)
    {
        Rol  = rol;
        Fase = FaseContractNet.Idle;
    }

    public void SetEstado(IEstadoConversacion nuevoEstado, FaseContractNet nuevaFase)
    {
        Fase = nuevaFase;
        base.SetEstado(nuevoEstado);
    }

    // Bloquea en Adjudicando (espera a InformDone) y Ejecutando
    public override bool BloqueaAgente() =>   Rol == RolContractNet.Contratista? Fase == FaseContractNet.Propose
        || Fase == FaseContractNet.Ejecutando : Fase == FaseContractNet.Adjudicando;

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
        // Debug.Log($"[Conv {ConversationId}] {emisor} rechazó participar.");
    }

    public bool ListoParaAdjudicar =>
        TotalPropuestasRecibidas >= TotalPropuestasEsperadas || Time.time >= Deadline;
}
