using UnityEngine;

// GestorEsperando  —  rol: Gestor (post-adjudicación)
// OnEntrar: registra cuántos InformDone espera de los contratistas asignados.
// Ejecutar: vacío.
// OnMensaje(InformDone): cuenta regresiva; cuando llega a 0 → Idle.
public class EstadoGestorEsperando : IEstadoConversacion
{
    private int informDonePendientes;

    public EstadoGestorEsperando(int numContratistasAsignados)
    {
        informDonePendientes = numContratistasAsignados;
    }

    public void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        Debug.Log($"[{capa.NombreAgente}] Gestor esperando {informDonePendientes} InformDone.");
        if (informDonePendientes <= 0)
            Transicion.A(capa, conv, new EstadoIdle(), FaseContractNet.Idle);
    }

    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }

    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg)
    {
        if (msg.performativa != Performativa.InformDone) return;

        informDonePendientes--;
        Debug.Log($"[{capa.NombreAgente}] InformDone de {msg.emisor} ({informDonePendientes} pendientes).");

        if (informDonePendientes <= 0)
            Transicion.A(capa, conv, new EstadoIdle(), FaseContractNet.Idle);
    }
}
