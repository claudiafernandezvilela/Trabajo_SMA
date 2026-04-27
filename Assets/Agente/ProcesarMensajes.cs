using System.Collections.Generic;
using UnityEngine;

/// Capa de comunicación FIPA-ACL.
/// Proporciona la infraestructura de mensajería que todos los agentes comparten.

public class ProcesarMensajes : MonoBehaviour
{
    public string nombreAgente = "Agente_1";

    private Queue<MensajeFIPA> colaMensajes = new Queue<MensajeFIPA>();

    // Historial de conversaciones 
    private Dictionary<string, List<MensajeFIPA>> historial
        = new Dictionary<string, List<MensajeFIPA>>();

    // Registro global estático
    // Permite localizar cualquier agente sin un coordinador central.
    private static Dictionary<string, ProcesarMensajes> registro
        = new Dictionary<string, ProcesarMensajes>();

    // Referencia al Cerebro
    private Cerebro cerebro;


    protected virtual void Awake()
    {
        cerebro = GetComponent<Cerebro>();
        registro[nombreAgente] = this;
    }

    protected virtual void Update()
    {
        ProcesarCola();
    }


    /// Envía un mensaje a un agente concreto o en broadcast (receptor == null).
    public void EnviarMensaje(MensajeFIPA mensaje)
    {
        mensaje.emisor = nombreAgente;

        if (string.IsNullOrEmpty(mensaje.receptor))
        {
            // Broadcast: entrega a todos los agentes registrados excepto a uno mismo
            foreach (var kvp in registro)
            {
                if (kvp.Key != nombreAgente)
                    kvp.Value.RecibirMensaje(mensaje);
            }
            Debug.Log($"[{nombreAgente}] BROADCAST → {mensaje}");
        }
        else
        {
            if (registro.TryGetValue(mensaje.receptor, out ProcesarMensajes destino))
            {
                destino.RecibirMensaje(mensaje);
                Debug.Log($"[{nombreAgente}] ENVÍO → {mensaje}");
            }
            else
            {
                Debug.LogWarning($"[{nombreAgente}] Receptor '{mensaje.receptor}' no encontrado.");
            }
        }
    }

    /// Encola el mensaje entrante y lo guarda en el historial.
    /// Llamado por el emisor a través de EnviarMensaje.
    public void RecibirMensaje(MensajeFIPA mensaje)
    {
        colaMensajes.Enqueue(mensaje);
        GuardarMensaje(mensaje);
        Debug.Log($"[{nombreAgente}] RECIBIDO ← {mensaje}");
    }


    /// Guarda el mensaje en el historial de su conversación.
    public void GuardarMensaje(MensajeFIPA mensaje)
    {
        if (!historial.ContainsKey(mensaje.conversationId))
            historial[mensaje.conversationId] = new List<MensajeFIPA>();

        historial[mensaje.conversationId].Add(mensaje);
    }


    //  Procesado de la cola
    /// Desencola todos los mensajes pendientes del frame actual, actualiza el ModeloMundo y notifica al Cerebro.
    private void ProcesarCola()
    {
        while (colaMensajes.Count > 0)
        {
            MensajeFIPA msg = colaMensajes.Dequeue();
            ActualizarModeloMundo(msg);
            NotificarCerebro(msg);
        }
    }


    /// Actualiza el ModeloMundo con los datos del mensaje.
    /// Solo modifica campos concretos (posición, flags); la interpretación
    /// del significado queda para las subclases y el Cerebro.
    protected virtual void ActualizarModeloMundo(MensajeFIPA msg)
    {
        ModeloMundo modelo = cerebro?.Modelo;
        if (modelo == null) return;

        // Si el mensaje trae una posición válida, la registramos como
        // última posición conocida del jugador (el significado exacto
        // lo decidirá la subclase sobrescribiendo este método).
        if (msg.posicion != Vector3.zero)
            modelo.ActualizarSonido(msg.posicion);
    }

    /// Notifica al Cerebro que ha llegado un mensaje para que sus capas
    /// reactiva y deliberativa puedan reaccionar.
    /// Las subclases pueden sobreescribir este método para añadir lógica.
    protected virtual void NotificarCerebro(MensajeFIPA msg)
    {
        // Por defecto no hace nada: la subclase decide cómo reaccionar.
        // Esto mantiene la clase base libre de lógica de dominio.
    }


    /// Devuelve el historial completo de una conversación
    public List<MensajeFIPA> ObtenerConversacion(string conversationId)
    {
        historial.TryGetValue(conversationId, out var lista);
        return lista ?? new List<MensajeFIPA>();
    }

    /// Número de mensajes pendientes en la cola.
    public int MensajesPendientes => colaMensajes.Count;

    /// Lista de todos los agentes actualmente registrados.
    public static IEnumerable<string> AgentesRegistrados() => registro.Keys;
}