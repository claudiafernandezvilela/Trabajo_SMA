using System.Collections.Generic;
using UnityEngine;
// Adjudicando  —  rol: Gestor
// OnEntrar: el gestor añade su propia oferta al pool (compitiendo con contratistas),
//           asigna cada tarea al mejor candidato disponible, y transita a
//           EsperandoComplecion para aguardar los InformDone de los contratistas.
public class EstadoAdjudicando : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv)
    {
        // El gestor evalúa su distancia y entra en el pool solo si debe competir.
        if (conv.GestorCompite)
        {
             for (int i = 0; i < conv.TareasDisponibles.Count; i++)
            {
                TareaData tarea = conv.TareasDisponibles[i];
                float distancia = Vector3.Distance(capa.transform.position, tarea.DestinoEjecucion);
                conv.RegistrarPropuesta(capa.NombreAgente, i, distancia);
            }
        }

        var asignados       = new HashSet<string>();
        int contratistasAsignados = 0;
        TareaData tareaGestor     = null;

        for (int i = 0; i < conv.TareasDisponibles.Count; i++)
        {
            TareaData tarea = conv.TareasDisponibles[i]; 
            if (!conv.PropuestasPorTarea.TryGetValue(i, out var propuestas)
                || propuestas.Count == 0)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Sin propuestas para tarea[{i}]={tarea.tipo}");
                continue;
            }
 
            // Ordenar por distancia ascendente; el gestor compite en igualdad de condiciones.
            propuestas.Sort((a, b) => a.puntuacion.CompareTo(b.puntuacion));
 
            string ganador = null;
            foreach (var p in propuestas)
            {
                if (!asignados.Contains(p.emisor)) { ganador = p.emisor; break; }
            }
 
            if (ganador == null)
            {
                Debug.LogWarning($"[{capa.NombreAgente}] Sin candidato libre para tarea[{i}]={tarea.tipo}");
                continue;
            }
 
            asignados.Add(ganador);

            if (ganador == capa.NombreAgente)
            {
                // El gestor es el mejor candidato: se autoasigna.
                tareaGestor = tarea;
                Debug.Log($"[{capa.NombreAgente}] Gestor autoasigna → {tarea.tipo}");
            }
            else
            {
                capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                    Performativa.AcceptProposal,
                    capa.NombreAgente, ganador,
                    tarea.Serializar(), tarea.posicionLadron,
                    conv.ConversationId));
                contratistasAsignados++;
                Debug.Log($"[{capa.NombreAgente}] Accept → {ganador} tarea:{tarea.tipo}");
            }

            // Reject al resto que propusieron para esta tarea (nunca al gestor mismo).
            foreach (var p in propuestas)
            {
                if (p.emisor == ganador || p.emisor == capa.NombreAgente) continue;
                if (asignados.Contains(p.emisor)) continue;
 
                capa.Mensajes.EnviarMensaje(new MensajeFIPA(
                    Performativa.RejectProposal,
                   capa.NombreAgente, p.emisor,
                    "no_requerido", Vector3.zero,
                    conv.ConversationId));
 
                Debug.Log($"[{capa.NombreAgente}] Reject → {p.emisor} tarea:{tarea.tipo}");
            }
        }
       
        // Activar comportamiento del gestor si se autoasignó una tarea.
        if (tareaGestor != null)
            capa.OnTareaAsignada(tareaGestor);

        // Esperar los InformDone de los contratistas (el gestor no reporta a nadie).
        Transicion.A(capa, conv,
            new EstadoGestorEsperando(contratistasAsignados),
            FaseContractNet.EsperandoComplecion);
    }
 
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }
    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}