using System.Collections.Generic;
using UnityEngine;

/// Capa de infraestructura de mensajería FIPA-ACL.
/// Responsabilidades:
///   - Registro global de agentes (diccionario estático).
///   - Enrutamiento físico de mensajes (broadcast o unicast).
///   - Cola de mensajes entrantes + historial por conversación.
///   - Actualización básica del ModeloMundo con datos del mensaje.
///   - Delegación a CapaComunicacion para la lógica de protocolo.
///
/// No sabe nada del ContractNet ni de las fases del protocolo.
public class ProcesarMensajes : MonoBehaviour
{
    public string nombreAgente = "Agente_1";

    // ── infraestructura ────────────────────────────────────────────────────

    private Queue<MensajeFIPA> colaMensajes = new Queue<MensajeFIPA>();

    private Dictionary<string, List<MensajeFIPA>> historial
        = new Dictionary<string, List<MensajeFIPA>>();

    /// Registro global estático: permite localizar cualquier agente
    /// sin necesidad de un coordinador central.
    private static Dictionary<string, ProcesarMensajes> registro
        = new Dictionary<string, ProcesarMensajes>();

    // ── limpieza entre ejecuciones de Play Mode ────────────────────────────

    /// Unity llama a este método estático antes de cargar la escena
    /// cada vez que se entra en Play Mode, limpiando entradas obsoletas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LimpiarRegistro()
    {
        registro.Clear();
    }

    // ── referencias ────────────────────────────────────────────────────────

    private Cerebro          cerebro;
    private CapaComunicacion capaComunicacion;

    // ── ciclo de vida Unity ────────────────────────────────────────────────

    protected virtual void Awake()
    {
        cerebro            = GetComponent<Cerebro>();
        capaComunicacion   = GetComponent<CapaComunicacion>();
        registro[nombreAgente] = this;
    }

    void OnDestroy()
    {
        if (registro.ContainsKey(nombreAgente))
            registro.Remove(nombreAgente);
    }

    protected virtual void Update()
    {
        ProcesarCola();
    }

    // ── envío ──────────────────────────────────────────────────────────────

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
                Debug.Log($"[{nombreAgente}] ENVÍO → {mensaje}");
            }
            else
            {
                Debug.LogWarning($"[{nombreAgente}] Receptor '{mensaje.receptor}' no encontrado.");
            }
        }
    }

    // ── recepción ──────────────────────────────────────────────────────────

    /// Encola el mensaje entrante. Llamado por el emisor a través de EnviarMensaje.
    public void RecibirMensaje(MensajeFIPA mensaje)
    {
        colaMensajes.Enqueue(mensaje);
        GuardarMensaje(mensaje);
        Debug.Log($"[{nombreAgente}] RECIBIDO ← {mensaje}");
    }

    // ── procesado de la cola ───────────────────────────────────────────────

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
    /// Solo toca campos de datos crudos (posición, flags).
    /// La semántica la interpretan CapaComunicacion y sus estados.
    protected virtual void ActualizarModeloMundo(MensajeFIPA msg)
    {
        ModeloMundo modelo = cerebro?.Modelo;
        if (modelo == null) return;

        // Cualquier mensaje con posición válida actualiza la última posición conocida.
        // Esto incluye el Inform de "ladron_escuchado" y el propio CFP.
        if (msg.posicion != Vector3.zero)
            modelo.ActualizarSonido(msg.posicion);
    }

    /// Delega el mensaje a CapaComunicacion para que gestione el protocolo.
    /// Es el único punto de contacto entre la infraestructura y la lógica del protocolo.
    private void NotificarCapa(MensajeFIPA msg)
    {
        capaComunicacion?.OnMensajeRecibido(msg);
    }

    // ── historial ──────────────────────────────────────────────────────────

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

    // ── utilidades estáticas ───────────────────────────────────────────────

    public int MensajesPendientes => colaMensajes.Count;

    /// Lista de todos los agentes actualmente registrados.
    /// Usada por EstadoCFP para registrar los agentes contactados.
    public static IEnumerable<string> AgentesRegistrados() => registro.Keys;
}