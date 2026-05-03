using System.Collections.Generic;
using UnityEngine;

public class ProcesarMensajes : MonoBehaviour
{
    public string nombreAgente = "Agente_1";

    private Queue<MensajeFIPA> colaMensajes = new Queue<MensajeFIPA>();

    private Dictionary<string, List<MensajeFIPA>> historial
        = new Dictionary<string, List<MensajeFIPA>>();

    /// Registro global estático: permite localizar cualquier agente sin necesidad de un coordinador central.
    private static Dictionary<string, ProcesarMensajes> registro
        = new Dictionary<string, ProcesarMensajes>();



    private Cerebro          cerebro;
    private CapaComunicacion capaComunicacion;

    protected virtual void Awake()
    {
        cerebro            = GetComponent<Cerebro>();
        capaComunicacion   = GetComponent<CapaComunicacion>();
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
                //Debug.Log($"[{nombreAgente}] ENVÍO → {mensaje}");
            }
            else
            {
                Debug.LogWarning($"[{nombreAgente}] Receptor '{mensaje.receptor}' no encontrado.");
            }
        }
    }


    /// Encola el mensaje entrante. Llamado por el emisor a través de EnviarMensaje.
    public void RecibirMensaje(MensajeFIPA mensaje)
    {
        colaMensajes.Enqueue(mensaje);
        GuardarMensaje(mensaje);
        // Debug.Log($"[{nombreAgente}] RECIBIDO ← {mensaje}");
    }

    private void ProcesarCola()
    {
        while (colaMensajes.Count > 0)
        {
            MensajeFIPA msg = colaMensajes.Dequeue();
            ActualizarModeloMundo(msg);
            NotificarCapa(msg);
        }
    }

    /// Actualiza el ModeloMundo con los datos del mensaje.
    protected virtual void ActualizarModeloMundo(MensajeFIPA msg)
    {
        ModeloMundo modelo = cerebro?.Modelo;
        if (modelo == null) return;

        // Posición del ladrón (escuchado o visto): actualiza última posición conocida.
        if (msg.posicion != Vector3.zero)
            modelo.ActualizarSonido(msg.posicion);
        
        // El objeto vigilado ha sido robado: actualizar base de conocimiento.
        if (msg.contenido == "objeto_robado")
            modelo.objetoRobado = true;
    }

    private void NotificarCapa(MensajeFIPA msg)
    {
        capaComunicacion?.OnMensajeRecibido(msg);
    }

    public void GuardarMensaje(MensajeFIPA mensaje)
    {
        if (!historial.ContainsKey(mensaje.conversationId))
            historial[mensaje.conversationId] = new List<MensajeFIPA>();

        historial[mensaje.conversationId].Add(mensaje);
    }

    public List<MensajeFIPA> ObtenerConversacion(string conversationId)
    {
        historial.TryGetValue(conversationId, out var lista);
        return lista ?? new List<MensajeFIPA>();
    }


    public int MensajesPendientes => colaMensajes.Count;

    /// Lista de todos los agentes actualmente registrados.
    public static IEnumerable<string> AgentesRegistrados() => registro.Keys;
    /// Devuelve las últimas n posiciones del ladrón registradas en el historial (de mensajes Inform propios y ajenos), ordenadas de más a menos reciente.
    public List<(Vector3 pos, float t)> ObtenerHistorialPosicionesLadron(int n)
    {
        var resultado = new List<(Vector3, float t)>();
        foreach (var msgs in historial.Values)
            foreach (var m in msgs)
                if ((m.contenido == "ladron_visto" || m.contenido == "ladron_escuchado")
                    && m.posicion != Vector3.zero)
                    resultado.Add((m.posicion, m.timestamp));

        resultado.Sort((a, b) => b.t.CompareTo(a.t));
        return resultado.Count > n ? resultado.GetRange(0, n) : resultado;
    }
}