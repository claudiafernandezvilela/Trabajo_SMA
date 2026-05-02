using System.Collections.Generic;
using UnityEngine;

/// Capa de comunicación de alto nivel.
/// Responsabilidades:
///   - Mantener las conversaciones ContractNet activas de este agente.
///   - Tickear los estados de conversación en Update.
///   - Recibir notificaciones de ProcesarMensajes y enrutarlas a la conversación correcta.
///   - Exponer la API de dominio (NotificarLadronEscuchado) que el Cerebro invoca.
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
    /// 2) Inicia un ContractNet como gestor con las tareas de búsqueda.
    public void NotificarLadronEscuchado(Vector3 posicion)
    {
        // 1) Inform broadcast: actualiza ultimaPosicionJugador en todos los agentes.
        //    ProcesarMensajes.ActualizarModeloMundo lo procesa en el receptor.
        var inform = new MensajeFIPA(
            Performativa.Inform,
            NombreAgente,
            null,           // broadcast
            "ladron_escuchado",
            posicion);

        Mensajes.EnviarMensaje(inform);

        // 2) Iniciar ContractNet solo si no hay ya una conversación activa como gestor.
        if (HayConversacionActivaComoGestor()) return;

        IniciarCFP(posicion);
    }

    // ── ContractNet: iniciador (gestor) ────────────────────────────────────

    /// Crea una nueva conversación ContractNet como gestor y emite el CFP.
    /// Las tareas se crean en orden de prioridad; el gestor asignará
    /// la tarea[0] al mejor proponente, tarea[1] al segundo, etc.
    private void IniciarCFP(Vector3 posicionLadron)
    {
        string convId = System.Guid.NewGuid().ToString("N").Substring(0, 8);

        var conv = new ConversacionContractNet(convId, RolContractNet.Gestor);

        // Consultar RegistroSalidas para obtener las 2 salidas más cercanas al ladrón.
        // El gestor es el único que hace este cálculo; el resultado viaja serializado
        // en las TareaData para que el contratista ganador sepa a dónde ir.
        Vector3 salida1 = Vector3.zero;
        Vector3 salida2 = Vector3.zero;

        if (RegistroSalidas.Instancia != null)
        {
            var cercanas = RegistroSalidas.Instancia.ObtenerMasCercanas(posicionLadron, 2);
            if (cercanas.Count >= 1) salida1 = cercanas[0].position;
            if (cercanas.Count >= 2) salida2 = cercanas[1].position;
        }
        else
        {
            Debug.LogWarning($"[{NombreAgente}] RegistroSalidas no encontrado en la escena.");
        }

        // Las 3 tareas se distribuyen por competencia: el gestor también evalúa
        // su distancia en EstadoAdjudicando y puede autoasignarse la que mejor domine.
        conv.TareasDisponibles.Add(new TareaData(TipoTarea.BloquearSalida1, posicionLadron, salida1));
        conv.TareasDisponibles.Add(new TareaData(TipoTarea.BloquearSalida2, posicionLadron, salida2));
        conv.TareasDisponibles.Add(new TareaData(TipoTarea.Buscar,          posicionLadron, Vector3.zero));

        conv.GestorId = NombreAgente;
        conversaciones[convId] = conv;
        Transicion.A(this, conv, new EstadoCFP(), FaseContractNet.CFP);

        Debug.Log($"[{NombreAgente}] CFP conv:{convId} salida1:{salida1} salida2:{salida2}");
    }

    // ContractNet: receptor (contratista)
    /// Crea una conversación como contratista al recibir un CFP.
    private void RecibirCFP(MensajeFIPA msg)
    {
        if (conversaciones.ContainsKey(msg.conversationId))
        {
            Debug.LogWarning($"[{NombreAgente}] CFP duplicado conv:{msg.conversationId}");
            return;
        }

        var conv = new ConversacionContractNet(msg.conversationId, RolContractNet.Contratista);

        // Guardamos el emisor del CFP (el gestor) en GestorId
        // para que EstadoPropose sepa a quién dirigir la propuesta.
        conv.GestorId = msg.emisor;

        // Deserializar las tareas del CFP (el contratista las conoce
        // para poder evaluar qué tarea podría hacer, aunque no decide cuál).
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

    /// ProcesarMensajes llama a este método cuando llega un MensajeFIPA
    /// cuyo conversationId apunta a una conversación de este agente,
    /// o cuando la performativa implica crear una nueva (CFP).
    public void OnMensajeRecibido(MensajeFIPA msg)
    {
        // Mensajes de información pura (Inform sin protocolo ContractNet):
        // ya fueron procesados por ProcesarMensajes.ActualizarModeloMundo.
        // No requieren gestión de conversación.
        if (msg.performativa == Performativa.Inform && msg.contenido == "ladron_escuchado")
            return;

        // Un CFP nuevo crea la conversación como contratista
        if (msg.performativa == Performativa.CFP)
        {
            RecibirCFP(msg);
            return;
        }

        // El resto de mensajes se enrutan a la conversación existente
        if (!conversaciones.TryGetValue(msg.conversationId, out var conv))
        {
            Debug.LogWarning($"[{NombreAgente}] Mensaje para conv desconocida:{msg.conversationId}");
            return;
        }

        conv.EstadoActual?.OnMensaje(this, conv, msg);

        // Limpiar conversaciones terminadas
        if (conv.Fase == FaseContractNet.Idle)
            conversaciones.Remove(conv.ConversationId);
    }

    // ── callback hacia Cerebro ─────────────────────────────────────────────

    /// Llamado por EstadoEsperandoRespuesta cuando el gestor acepta la propuesta.
    /// Traduce la tarea ContractNet al objetivo del Deliberativo.
    public void OnTareaAsignada(TareaData tarea)
    {
        Debug.Log($"[{NombreAgente}] Tarea asignada: {tarea.tipo} destino:{tarea.DestinoEjecucion}");

        switch (tarea.tipo)
        {
            case TipoTarea.BloquearSalida1:
            case TipoTarea.BloquearSalida2:
                // El destino ya viene calculado en la TareaData por el gestor.
                // Lo inyectamos directamente en el estado BloquearSalida.
                cerebro.CambiarABloquearSalida(tarea.DestinoEjecucion);
                break;

            case TipoTarea.Buscar:
                // La posición del ladrón ya está en ultimaPosicionJugador
                // (actualizada por el Inform broadcast). No hace falta más.
                deliberativo.EstablecerObjetivo(Objetivo.Buscar);
                break;
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────

    /// Llamado por Cerebro cuando un estado de comportamiento ContractNet termina.
    /// Busca la conversación activa en fase Ejecutando y envía InformDone al gestor.
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
    /// Evita iniciar dos ContractNet simultáneos por el mismo evento.
    private bool HayConversacionActivaComoGestor()
    {
        foreach (var conv in conversaciones.Values)
            if (conv.Rol == RolContractNet.Gestor && conv.Fase != FaseContractNet.Idle)
                return true;
        return false;
    }
}