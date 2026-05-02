using System.Collections.Generic;
using UnityEngine;

/// Capa de comunicación de alto nivel.
/// Responsabilidades:
///   - Mantener las conversaciones ContractNet activas de este agente.
///   - Tickear los estados de conversación en Update.
///   - Recibir notificaciones de ProcesarMensajes y enrutarlas a la conversación correcta.
///   - Exponer la API de dominio que el Cerebro invoca.
///   - Notificar al Cerebro cuando se le asigna una tarea (callback OnTareaAsignada).
///
/// Lo que NO hace:
///   - Nunca enruta mensajes directamente: siempre usa ProcesarMensajes.
///   - Nunca decide qué comportamiento adoptar: eso lo decide Cerebro/Deliberativo.
///   - No sabe nada de NavMesh, sensores ni de la lógica de juego.
public class CapaComunicacion : MonoBehaviour
{
    // ── referencias ────────────────────────────────────────────────────────

    /// Acceso público para que los estados de conversación puedan enviar mensajes.
    public ProcesarMensajes Mensajes { get; private set; }

    /// Nombre de este agente (delegado a ProcesarMensajes para evitar duplicidad).
    public string NombreAgente => Mensajes.nombreAgente;

    private Cerebro           cerebro;
    private CerebroDeliberativo deliberativo;

    // ── conversaciones activas ─────────────────────────────────────────────
    // Clave: conversationId. Un agente puede estar en varias conversaciones a la vez
    // (p.ej. como contratista en una y como gestor en otra).
    private readonly Dictionary<string, ConversacionContractNet> conversaciones
        = new Dictionary<string, ConversacionContractNet>();

    // ── estado de visión ───────────────────────────────────────────────────

    /// Evita lanzar un segundo CFP de visión mientras el primero sigue activo.
    private bool  cfpVisionActivo = false;

    /// Rate-limit: segundos mínimos entre broadcasts de posición del ladrón.
    private float tiempoUltimaBroadcastPosicion = -999f;
    private const float IntervaloMinBroadcast = 0.5f;

    // ── ciclo de vida Unity ────────────────────────────────────────────────

    void Awake()
    {
        Mensajes     = GetComponent<ProcesarMensajes>();
        cerebro      = GetComponent<Cerebro>();
        deliberativo = GetComponent<CerebroDeliberativo>();
    }

    /// Tickea el estado activo de cada conversación viva.
    void Update()
    {
        foreach (var conv in conversaciones.Values)
            conv.EstadoActual?.Ejecutar(this, conv);
    }

    // ── API pública de dominio ─────────────────────────────────────────────

    /// Llamado por Cerebro.OnPlayerHeard.
    /// 1) Emite un Inform broadcast para que todos actualicen su ModeloMundo.
    /// 2) Inicia un ContractNet como gestor con 3 tareas (BS1, BS2, Buscar).
    ///    El gestor también compite por las tareas.
    public void NotificarLadronEscuchado(Vector3 posicion)
    {
        Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.Inform, NombreAgente, null, "ladron_escuchado", posicion));

        if (HayConversacionActivaComoGestor()) return;

        var (s1, s2) = ObtenerSalidasCercanas(posicion);
        IniciarCFP(new List<TareaData>
        {
            new TareaData(TipoTarea.BloquearSalida1, posicion, s1),
            new TareaData(TipoTarea.BloquearSalida2, posicion, s2),
            new TareaData(TipoTarea.Buscar,          posicion, Vector3.zero)
        }, gestorCompite: true);
    }

    /// Llamado por Cerebro.OnPlayerSeen cada frame (rate-limited internamente).
    /// 1) Emite un Inform broadcast con la posición actual del ladrón.
    /// 2) Inicia un ContractNet con 2 tareas (BS1, BS2) si no hay uno activo.
    ///    El gestor NO compite porque está persiguiendo al ladrón.
    public void NotificarLadronVisto(Vector3 posicion)
    {
        // Broadcast de posición con rate-limit para no saturar la red.
        if (Time.time - tiempoUltimaBroadcastPosicion >= IntervaloMinBroadcast)
        {
            tiempoUltimaBroadcastPosicion = Time.time;
            Mensajes.EnviarMensaje(new MensajeFIPA(
                Performativa.Inform, NombreAgente, null, "ladron_visto", posicion));
        }

        // Un único CFP por episodio de visión.
        if (cfpVisionActivo || HayConversacionActivaComoGestor()) return;
        cfpVisionActivo = true;

        var (s1, s2) = ObtenerSalidasCercanas(posicion);
        IniciarCFP(new List<TareaData>
        {
            new TareaData(TipoTarea.BloquearSalida1, posicion, s1),
            new TareaData(TipoTarea.BloquearSalida2, posicion, s2)
        }, gestorCompite: false);
    }

    /// Llamado por Cerebro.OnPlayerLost. Permite lanzar un nuevo CFP de visión
    /// la próxima vez que el ladrón vuelva a ser avistado.
    public void NotificarLadronPerdido()
    {
        cfpVisionActivo = false;
    }

    /// Llamado por Cerebro.NotificarObjetoRobado.
    /// Emite un Inform broadcast para que todos sepan que el objeto está robado.
    public void NotificarObjetoRobadoBroadcast()
    {
        Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.Inform, NombreAgente, null, "objeto_robado", Vector3.zero));
        Debug.Log($"[{NombreAgente}] Broadcast: objeto_robado.");
    }

    // ── ContractNet: iniciador (gestor) ────────────────────────────────────

    /// Crea una nueva conversación ContractNet como gestor y emite el CFP.
    private void IniciarCFP(List<TareaData> tareas, bool gestorCompite)
    {
        string convId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        var conv = new ConversacionContractNet(convId, RolContractNet.Gestor);
        conv.GestorId      = NombreAgente;
        conv.GestorCompite = gestorCompite;
        foreach (var t in tareas) conv.TareasDisponibles.Add(t);
        conversaciones[convId] = conv;
        Transicion.A(this, conv, new EstadoCFP(), FaseContractNet.CFP);
        Debug.Log($"[{NombreAgente}] CFP conv:{convId} tareas:{tareas.Count} gestorCompite:{gestorCompite}");
    }

    // ── ContractNet: receptor (contratista) ────────────────────────────────

    /// Crea una conversación como contratista al recibir un CFP.
    private void RecibirCFP(MensajeFIPA msg)
    {
        if (conversaciones.ContainsKey(msg.conversationId))
        {
            Debug.LogWarning($"[{NombreAgente}] CFP duplicado conv:{msg.conversationId}");
            return;
        }

        var conv = new ConversacionContractNet(msg.conversationId, RolContractNet.Contratista);
        conv.GestorId = msg.emisor;

        if (!string.IsNullOrEmpty(msg.contenido))
        {
            foreach (string tareaStr in msg.contenido.Split(';'))
            {
                TareaData t = TareaData.Deserializar(tareaStr);
                if (t != null) conv.TareasDisponibles.Add(t);
            }
        }

        conversaciones[msg.conversationId] = conv;
        Transicion.A(this, conv, new EstadoPropose(msg.posicion), FaseContractNet.CFP);
    }

    // ── punto de entrada de mensajes (llamado por ProcesarMensajes) ────────

    public void OnMensajeRecibido(MensajeFIPA msg)
    {
        // Mensajes Inform puros: ya procesados por ProcesarMensajes.ActualizarModeloMundo.
        if (msg.performativa == Performativa.Inform &&
            (msg.contenido == "ladron_escuchado" ||
             msg.contenido == "ladron_visto"     ||
             msg.contenido == "objeto_robado"))
            return;

        if (msg.performativa == Performativa.CFP)
        {
            RecibirCFP(msg);
            return;
        }

        if (!conversaciones.TryGetValue(msg.conversationId, out var conv))
        {
            Debug.LogWarning($"[{NombreAgente}] Mensaje para conv desconocida:{msg.conversationId}");
            return;
        }

        conv.EstadoActual?.OnMensaje(this, conv, msg);

        if (conv.Fase == FaseContractNet.Idle)
            conversaciones.Remove(conv.ConversationId);
    }

    // ── callback hacia Cerebro ─────────────────────────────────────────────

    /// Llamado por EstadoEsperandoRespuesta cuando el gestor acepta la propuesta.
    public void OnTareaAsignada(TareaData tarea)
    {
        Debug.Log($"[{NombreAgente}] Tarea asignada: {tarea.tipo} destino:{tarea.DestinoEjecucion}");

        switch (tarea.tipo)
        {
            case TipoTarea.BloquearSalida1:
            case TipoTarea.BloquearSalida2:
                cerebro.CambiarABloquearSalida(tarea.DestinoEjecucion);
                break;

            case TipoTarea.Buscar:
                deliberativo.EstablecerObjetivo(Objetivo.Buscar);
                break;
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────

    /// Llamado por Cerebro cuando un estado de comportamiento ContractNet termina.
    public void NotificarTareaCompletada()
    {
        foreach (var conv in conversaciones.Values)
        {
            if (conv.Fase == FaseContractNet.Ejecutando && conv.EstadoActual is EstadoEjecutando ejec)
            {
                ejec.NotificarCompletada(this, conv);
                return;
            }
        }
    }

    /// True si el agente está en un estado que le impide participar en un ContractNet.
    public bool EstaOcupado()
    {
        if (deliberativo.ObjetivoActual == Objetivo.Perseguir) return true;
        foreach (var conv in conversaciones.Values)
            if (conv.Fase == FaseContractNet.Ejecutando ||
                conv.Fase == FaseContractNet.EsperandoComplecion) return true;
        return false;
    }

    /// True si ya existe una conversación activa en la que este agente es gestor.
    private bool HayConversacionActivaComoGestor()
    {
        foreach (var conv in conversaciones.Values)
            if (conv.Rol == RolContractNet.Gestor && conv.Fase != FaseContractNet.Idle)
                return true;
        return false;
    }

    /// Devuelve las 2 salidas más cercanas a una posición dada.
    private (Vector3 s1, Vector3 s2) ObtenerSalidasCercanas(Vector3 posicion)
    {
        Vector3 s1 = Vector3.zero, s2 = Vector3.zero;
        if (RegistroSalidas.Instancia != null)
        {
            var cercanas = RegistroSalidas.Instancia.ObtenerMasCercanas(posicion, 2);
            if (cercanas.Count >= 1) s1 = cercanas[0].position;
            if (cercanas.Count >= 2) s2 = cercanas[1].position;
        }
        else
        {
            Debug.LogWarning($"[{NombreAgente}] RegistroSalidas no encontrado en la escena.");
        }
        return (s1, s2);
    }
}
