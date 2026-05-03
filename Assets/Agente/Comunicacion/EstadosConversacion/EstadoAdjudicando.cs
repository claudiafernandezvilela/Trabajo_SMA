using System.Collections.Generic;
using UnityEngine;
// Adjudicando  —  rol: Gestor
// OnEntrar: evalúa propuestas, envía Accept/Reject, se autoasigna si es el mejor candidato.
//           Cuenta cuántos contratistas fueron asignados → informDonePendientes.
//           Si nadie más fue asignado, cierra la conversación directamente.
// Ejecutar: vacío.
// OnMensaje(InformDone): cuenta regresiva; cuando llega a 0 → Idle.
public class EstadoAdjudicando : IEstadoConversacion
{
    private int informDonePendientes;
    private float deadline;


    public void OnEntrar(CapaComunicacion capa, Conversacion conv)
    {
        deadline = Time.time + 30f; // timeout de seguridad
        var cnet = (ConvCNet)conv;

        if (cnet.GestorCompite)
        {
            for (int i = 0; i < cnet.TareasDisponibles.Count; i++)
            {
                TareaData tarea = cnet.TareasDisponibles[i];
                float distancia = Vector3.Distance(capa.transform.position, tarea.DestinoEjecucion);
                cnet.RegistrarPropuesta(capa.NombreAgente, i, distancia);
            }
        }

        var asignados = new HashSet<string>();
        int contratistasAsignados = 0;
        TareaData tareaGestor = null;

        for (int i = 0; i < cnet.TareasDisponibles.Count; i++)
        {
            TareaData tarea = cnet.TareasDisponibles[i];
            if (!cnet.PropuestasPorTarea.TryGetValue(i, out var propuestas) || propuestas.Count == 0)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Sin propuestas para tarea[{i}]={tarea.tipo}");
                continue;
            }

            propuestas.Sort((a, b) => a.puntuacion.CompareTo(b.puntuacion));

            string ganador = null;
            foreach (var p in propuestas)
                if (!asignados.Contains(p.emisor)) { ganador = p.emisor; break; }

            if (ganador == null)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Sin candidato libre para tarea[{i}]={tarea.tipo}");
                continue;
            }

            asignados.Add(ganador);

            if (ganador == capa.NombreAgente)
            {
                tareaGestor = tarea;
                Debug.Log($"[{capa.NombreAgente}] Gestor autoasigna → {tarea.tipo}");
            }
            else
            {
                capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                    Performativa.AcceptProposal,
                    capa.NombreAgente, ganador,
                    tarea.Serializar(), tarea.posicionLadron,
                    cnet.ConversationId));
                contratistasAsignados++;
                Debug.Log($"[{capa.NombreAgente}] Accept → {ganador} tarea:{tarea.tipo}");
            }

            foreach (var p in propuestas)
            {
                if (p.emisor == ganador || p.emisor == capa.NombreAgente) continue;
                if (asignados.Contains(p.emisor)) continue;

                capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                    Performativa.RejectProposal,
                    capa.NombreAgente, p.emisor,
                    "no_requerido", Vector3.zero,
                    cnet.ConversationId));

                // Debug.Log($"[{capa.NombreAgente}] Reject → {p.emisor} tarea:{tarea.tipo}");
            }
        }

        if (tareaGestor != null)
            capa.OnTareaAsignada(tareaGestor);

        informDonePendientes = contratistasAsignados;
        Debug.Log($"[{capa.NombreAgente}] Adjudicando: esperando {informDonePendientes} InformDone.");

        if (informDonePendientes <= 0)
            Transicion.A(capa, cnet, new EstadoIdle(), FaseContractNet.Idle);
    }

    public void Ejecutar(CapaComunicacion capa, Conversacion conv) { 
        if (Time.time < deadline) return;
        Debug.LogWarning($"[{capa.NombreAgente}] Timeout esperando InformDone");
        Transicion.A(capa, (ConvCNet)conv, new EstadoIdle(), FaseContractNet.Idle);}

    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg)
    {
        if (msg.performativa != Performativa.InformDone) return;

        var cnet = (ConvCNet)conv;
        informDonePendientes--;
        Debug.Log($"[{capa.NombreAgente}] InformDone de {msg.emisor} ({informDonePendientes} pendientes).");

        if (informDonePendientes <= 0)
            Transicion.A(capa, cnet, new EstadoIdle(), FaseContractNet.Idle);
    }
}
