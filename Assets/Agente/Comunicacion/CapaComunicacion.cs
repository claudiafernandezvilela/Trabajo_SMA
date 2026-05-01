using System.Collections.Generic;
using UnityEngine;

// ============================================================
// ENUMS DEL PROTOCOLO CONTRACT-NET
// ============================================================

/// <summary>
/// Fase de la conversación en el protocolo Contract-Net.
/// Cada agente avanza por estas fases según los mensajes que recibe/envía.
/// El ROL (gestor/contratista) lo determina quién inició la conversación,
/// no un campo fijo del agente: el que envió el CFP es gestor,
/// el que lo recibió es contratista.
/// </summary>
public enum FaseConversacion
{
    Idle,           // Sin protocolo activo — puede aceptar nuevos CFPs
    CFP,            // Gestor: acaba de emitir el CFP y espera propuestas
                    // Contratista: está evaluando si proponer o rechazar
    Propose,        // Gestor: esperando propuestas hasta el deadline
                    // Contratista: ha enviado Propose, espera respuesta
    Adjudicando,    // Gestor: evaluando propuestas recibidas
                    // Contratista: esperando Accept o Reject
    Ejecutando      // Contratista: ejecutando la tarea asignada
                    // Gestor: esperando InformDone / InformResult / Failure
}

/// Rol del agente dentro de una conversación concreta.
public enum RolConversacion { Ninguno, Gestor, Contratista }

/// Tipos de tarea que el gestor puede repartir.
/// Se serializa como string en el campo 'contenido' del MensajeFIPA.
public enum Tareas
{
    BloquearSalidaPrimaria,    // Puerta más cercana al ladrón
    BloquearSalidaSecundaria,  // Segunda puerta más cercana
    Buscar                     // Ir a la última posición conocida y buscar
}


// DATOS DE CONVERSACIÓN
/// Un agente puede tener a lo sumo UNA conversación activa a la vez
public class DatosConversacion
{
    public string       ConversationId   { get; set; }
    public RolConversacion Rol           { get; set; } = RolConversacion.Ninguno;
    public FaseConversacion Fase         { get; set; } = FaseConversacion.Idle;

    // Datos que solo usa el GESTOR
    public List<string>              ContratistasEsperados { get; set; } = new List<string>();
    public Dictionary<string, float> Propuestas            { get; set; } = new Dictionary<string, float>(); // agente → coste
    public float                     Deadline              { get; set; }   // Time.time hasta el que espera propuestas
    public Vector3                   PosicionLadron        { get; set; }
    public List<Vector3>             PuertasCercanas       { get; set; } = new List<Vector3>(); // ordenadas por distancia al ladrón

    // Datos que solo usa el CONTRATISTA 
    public Tareas TareaAsignada  { get; set; }
    public Vector3   DestinoTarea   { get; set; }   // posición de la puerta o del ladrón
    public string    Gestor         { get; set; }   // nombre del gestor que le envió el CFP

    public void Reset()
    {
        ConversationId      = null;
        Rol                 = RolConversacion.Ninguno;
        Fase                = FaseConversacion.Idle;
        ContratistasEsperados.Clear();
        Propuestas.Clear();
        Deadline            = 0f;
        PosicionLadron      = Vector3.zero;
        PuertasCercanas.Clear();
        TareaAsignada       = Tareas.Buscar;
        DestinoTarea        = Vector3.zero;
        Gestor              = null;
    }
}


// CAPA DE COMUNICACIÓN

/// Capa de comunicación unificada que implementa el protocolo FIPA Contract-Net.

///   • Un agente tiene UNA conversación activa a la vez.
///   • El rol (gestor/contratista) se determina al recibir/enviar el CFP
///     y se almacena en DatosConversacion, no en el componente.
///   • El gestor puede cambiar entre protocolos: quien activa el evento
///     inicia el protocolo y se convierte en gestor de esa conversación.
[RequireComponent(typeof(ProcesarMensajes))]
[RequireComponent(typeof(Cerebro))]
public class CapaComunicacion : MonoBehaviour
{
    // Configuración inspector
    [Header("ContractNet Settings")]
    [Tooltip("Tiempo máximo (segundos) que el gestor espera propuestas")]
    public float tiempoEsperaPropuestas = 2f;

    [Tooltip("Puertas")]
    public Transform[] puertas;


    // Estado interno
    private DatosConversacion conv = new DatosConversacion();
    private ProcesarMensajes  com;
    private Cerebro           cerebro;


    // Acceso de solo lectura para otros componentes

    public FaseConversacion   FaseActual    => conv.Fase;
    public RolConversacion    RolActual     => conv.Rol;
    public string             ConvId        => conv.ConversationId;

    void Awake()
    {
        com   = GetComponent<ProcesarMensajes>();
        cerebro = GetComponent<Cerebro>();
    }

    void Update()
    {
        // El gestor comprueba el deadline de la fase Propose
        if (conv.Rol == RolConversacion.Gestor && conv.Fase == FaseConversacion.Propose)
        {
            if (Time.time >= conv.Deadline)
            {
                Debug.Log($"[CapaCom][{com.nombreAgente}] Deadline alcanzado — adjudicando con {conv.Propuestas.Count} propuesta(s).");
                Adjudicar();
            }
        }
    }


    // API PÚBLICA — INICIO DEL PROTOCOLO (iniciativa propia)

    /// Inicia el protocolo Contract-Net como GESTOR.
    /// Llamado por ProcesarMensajes cuando se detecta que el ladrón fue escuchado.
    public void IniciarProtocoloEscucha(Vector3 posicionLadron)
    {
        if (conv.Fase != FaseConversacion.Idle)
        {
            Debug.LogWarning($"[CapaCom][{com.nombreAgente}] Ya hay un protocolo activo ({conv.Fase}). Ignorando IniciarProtocoloEscucha.");
            return;
        }

        // Calcular las 2 puertas más cercanas al ladrón
        List<Vector3> puertasOrdenadas = ObtenerPuertasOrdenadasPorDistancia(posicionLadron);

        // Configurar la conversación como GESTOR
        conv.Reset();
        conv.ConversationId  = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        conv.Rol             = RolConversacion.Gestor;
        conv.Fase            = FaseConversacion.CFP;
        conv.PosicionLadron  = posicionLadron;
        conv.PuertasCercanas = puertasOrdenadas;

        // Registrar los contratistas esperados
        foreach (string nombre in ProcesarMensajes.AgentesRegistrados())
        {
            if (nombre != com.nombreAgente)
                conv.ContratistasEsperados.Add(nombre);
        }

        Debug.Log($"[CapaCom][{com.nombreAgente}] Iniciando ContractNet como GESTOR. Conv={conv.ConversationId}");
        EmitirCFPs(posicionLadron, puertasOrdenadas);
    }

    // API PÚBLICA — ENTRADA DE MENSAJES (llamada desde ProcesarMensajes)

    /// Punto de entrada unificado para mensajes ContractNet.
    /// ProcesarMensajes lo llama cuando detecta un mensaje de esta familia.
    public void ProcesarMensajeCN(MensajeFIPA msg)
    {
        switch (msg.performativa)
        {
            case Performativa.CFP:            OnCFPRecibido(msg);             break;
            case Performativa.Refuse:         OnRefuseRecibido(msg);          break;
            case Performativa.Propose:        OnPropuestaRecibida(msg);       break;
            case Performativa.AcceptProposal: OnAcceptProposalRecibido(msg);  break;
            case Performativa.RejectProposal: OnRejectProposalRecibido(msg);  break;
            case Performativa.InformDone:     OnInformDoneRecibido(msg);      break;
            case Performativa.Failure:        OnFailureRecibido(msg);         break;
            default:
                Debug.LogWarning($"[CapaCom][{com.nombreAgente}] Performativa inesperada: {msg.performativa}");
                break;
        }
    }

    /// Permite a BloquearSalida notificar que la tarea ha concluido.
    public void NotificarTareaCompletada()
    {
        if (conv.Rol != RolConversacion.Contratista || conv.Fase != FaseConversacion.Ejecutando) return;

        EnviarMensaje(new MensajeFIPA(
            Performativa.InformDone,
            com.nombreAgente,
            conv.Gestor,
            "TareaCompletada",
            conversationId: conv.ConversationId,
            inReplyTo:      conv.ConversationId));

        conv.Reset();
        Debug.Log($"[CapaCom][{com.nombreAgente}] InformDone enviado. Volviendo a Idle.");
    }

    /// Fuerza la fase (usado por BloquearSalida al llegar a destino).
    public void SetFase(FaseConversacion fase)
    {
        conv.Fase = fase;
    }

        public void OnTareaEjecutadaCompletada()
    {
        if (conv.Rol != RolConversacion.Contratista || conv.Fase != FaseConversacion.Ejecutando) return;

        EnviarMensaje(new MensajeFIPA(
            Performativa.InformDone,
            comms.nombreAgente,
            conv.Gestor,
            "TareaCompletada",
            conversationId: conv.ConversationId,
            inReplyTo:      conv.ConversationId));

        conv.Reset();
        Debug.Log($"[CapaCom][{comms.nombreAgente}] InformDone enviado. Volviendo a Idle.");
    }

    // LÓGICA DE GESTOR

    private void EmitirCFPs(Vector3 posicionLadron, List<Vector3> puertasCercanas)
    {
        // Tarea 1: BloquearSalidaPrimaria   → coste = distancia del contratista a puerta[0]
        // Tarea 2: BloquearSalidaSecundaria → coste = distancia del contratista a puerta[1]
        // Tarea 3: Buscar                   → coste = distancia del contratista al ladrón
        //
        // Enviamos UN CFP por cada tarea como broadcast.
        // El campo 'contenido' codifica: "TAREA|posX,posY,posZ"
        // El campo 'replyWith' es el conversationId para que los contratistas lo usen en inReplyTo.

        string convId = conv.ConversationId;

        // CFP para BloquearSalidaPrimaria
        EnviarBroadcastCFP(Tareas.BloquearSalidaPrimaria, puertasCercanas[0], convId);

        // CFP para BloquearSalidaSecundaria
        EnviarBroadcastCFP(Tareas.BloquearSalidaSecundaria, puertasCercanas[1], convId);

        // CFP para Buscar (destino = posición del ladrón)
        EnviarBroadcastCFP(Tareas.Buscar, posicionLadron, convId);

        conv.Fase    = FaseConversacion.Propose;
        conv.Deadline = Time.time + tiempoEsperaPropuestas;
    }

    private void EnviarBroadcastCFP(Tareas tarea, Vector3 destino, string convId)
    {
        string contenido = CodificarTarea(tarea, destino);
        MensajeFIPA cfp  = new MensajeFIPA(
            Performativa.CFP,
            com.nombreAgente,
            null,                   // broadcast
            contenido,
            posicion:       destino,
            conversationId: convId,
            replyWith:      convId);
        com.EnviarMensaje(cfp);
    }

    private void OnRefuseRecibido(MensajeFIPA msg)
    {
        if (!EsMiConversacion(msg)) return;
        if (conv.Rol != RolConversacion.Gestor) return;

        Debug.Log($"[CapaCom][{com.nombreAgente}] REFUSE recibido de {msg.emisor}: {msg.contenido}");
        // Quitamos al contratista de la lista de esperados (ya respondió negativamente)
        conv.ContratistasEsperados.Remove(msg.emisor);
        ComprobarSiTodosRespondieron();
    }

    private void OnPropuestaRecibida(MensajeFIPA msg)
    {
        if (!EsMiConversacion(msg)) return;
        if (conv.Rol != RolConversacion.Gestor) return;
        if (conv.Fase != FaseConversacion.Propose) return;

        // El contenido codifica: "TAREA|coste"
        if (!TryParsePropuesta(msg.contenido, out Tareas tarea, out float coste)) return;

        string clave = $"{msg.emisor}|{tarea}";
        if (!conv.Propuestas.ContainsKey(clave))
            conv.Propuestas[clave] = coste;

        conv.ContratistasEsperados.Remove(msg.emisor);
        Debug.Log($"[CapaCom][{com.nombreAgente}] PROPOSE de {msg.emisor}: tarea={tarea} coste={coste:F1}");
        ComprobarSiTodosRespondieron();
    }

    private void ComprobarSiTodosRespondieron()
    {
        // Si ya no esperamos a nadie (o el deadline lo forzará), adjudicamos
        if (conv.ContratistasEsperados.Count == 0)
        {
            Debug.Log($"[CapaCom][{com.nombreAgente}] Todos respondieron — adjudicando.");
            Adjudicar();
        }
    }
    private void Adjudicar()
    {
        conv.Fase = FaseConversacion.Adjudicando;

        // Reconstruir propuestas por tarea
        var porTarea = new Dictionary<Tareas, Dictionary<string, float>>();
        foreach (var kv in conv.Propuestas)
        {
            string[] partes = kv.Key.Split('|');
            if (partes.Length != 2) continue;
            string agente = partes[0];
            if (!System.Enum.TryParse(partes[1], out Tareas tarea)) continue;

            if (!porTarea.ContainsKey(tarea))
                porTarea[tarea] = new Dictionary<string, float>();
            porTarea[tarea][agente] = kv.Value;
        }

        var asignados = new HashSet<string>();
        var adjudicaciones = new Dictionary<Tareas, string>(); // tarea → agente ganador

        // Jerarquía de asignación
        Tareas[] jerarquia = {
            Tareas.BloquearSalidaPrimaria,
            Tareas.BloquearSalidaSecundaria,
            Tareas.Buscar
        };

        foreach (Tareas tarea in jerarquia)
        {
            if (!porTarea.TryGetValue(tarea, out var candidatos)) continue;

            string mejor    = null;
            float  mejorVal = float.MaxValue;

            foreach (var kv in candidatos)
            {
                if (asignados.Contains(kv.Key)) continue;
                if (kv.Value < mejorVal)
                {
                    mejorVal = kv.Value;
                    mejor    = kv.Key;
                }
            }

            if (mejor != null)
            {
                adjudicaciones[tarea] = mejor;
                asignados.Add(mejor);
            }
        }

        // Enviar Accept a ganadores / Reject a perdedores
        foreach (var kv in conv.Propuestas)
        {
            string[] partes = kv.Key.Split('|');
            string agente   = partes[0];
            if (!System.Enum.TryParse(partes[1], out Tareas tarea)) continue;

            bool esGanador = adjudicaciones.TryGetValue(tarea, out string ganador) && ganador == agente;

            if (esGanador)
            {
                // Construir contenido con la tarea y el destino
                Vector3 destino = tarea == Tareas.Buscar
                    ? conv.PosicionLadron
                    : (tarea == Tareas.BloquearSalidaPrimaria ? conv.PuertasCercanas[0] : conv.PuertasCercanas[1]);

                string contenido = CodificarTarea(tarea, destino);
                com.EnviarMensaje(new MensajeFIPA(
                    Performativa.AcceptProposal,
                    com.nombreAgente,
                    agente,
                    contenido,
                    posicion:       destino,
                    conversationId: conv.ConversationId,
                    inReplyTo:      conv.ConversationId));

                Debug.Log($"[CapaCom][{com.nombreAgente}] ACCEPT → {agente} tarea={tarea}");
            }
            else
            {
                com.EnviarMensaje(new MensajeFIPA(
                    Performativa.RejectProposal,
                    com.nombreAgente,
                    agente,
                    "OtroContratistaMejorOpcíón",
                    conversationId: conv.ConversationId,
                    inReplyTo:      conv.ConversationId));

                Debug.Log($"[CapaCom][{com.nombreAgente}] REJECT → {agente} tarea={tarea}");
            }
        }

        // El gestor pasa a Ejecutando (espera InformDone de los ganadores)
        conv.Fase = FaseConversacion.Ejecutando;

        // Si no hubo ninguna adjudicación (nadie propuso), volvemos a Idle
        if (adjudicaciones.Count == 0)
        {
            Debug.LogWarning($"[CapaCom][{com.nombreAgente}] Ninguna propuesta recibida. Volviendo a Idle.");
            conv.Reset();
        }
    }

    private void OnInformDoneRecibido(MensajeFIPA msg)
    {
        if (!EsMiConversacion(msg)) return;
        if (conv.Rol != RolConversacion.Gestor) return;

        Debug.Log($"[CapaCom][{com.nombreAgente}] InformDone de {msg.emisor}. Protocolo completado.");
        // Por simplicidad: cuando recibimos el primer InformDone damos el protocolo por cerrado.
        conv.Reset();
    }

    private void OnFailureRecibido(MensajeFIPA msg)
    {
        if (!EsMiConversacion(msg)) return;
        if (conv.Rol != RolConversacion.Gestor) return;

        Debug.LogWarning($"[CapaCom][{com.nombreAgente}] FAILURE de {msg.emisor}: {msg.contenido}");
        conv.Reset();
    }


    // LÓGICA DE CONTRATISTA
    private void OnCFPRecibido(MensajeFIPA msg)
    {
        // Un agente no se propone a sí mismo
        if (msg.emisor == com.nombreAgente) return;

        // Si ya estamos en un protocolo activo (no Idle) rechazamos
        if (conv.Fase != FaseConversacion.Idle)
        {
            EnviarMensaje(new MensajeFIPA(
                Performativa.Refuse,
                com.nombreAgente,
                msg.emisor,
                "OcupadoEnOtroProtocolo",
                conversationId: msg.conversationId,
                inReplyTo:      msg.replyWith));
            return;
        }

        // Parsear tarea del contenido
        if (!TryParseTareaDesdeCFP(msg.contenido, out Tareas tarea, out Vector3 destino))
        {
            EnviarMensaje(new MensajeFIPA(
                Performativa.Refuse,
                com.nombreAgente,
                msg.emisor,
                "ContenidoInvalido",
                conversationId: msg.conversationId,
                inReplyTo:      msg.replyWith));
            return;
        }

        // Si el reactivo está en Perseguir (prioridad máxima), rechazamos
        if (cerebro.Deliberativo.ObjetivoActual == Objetivo.Perseguir)
        {
            EnviarMensaje(new MensajeFIPA(
                Performativa.Refuse,
                com.nombreAgente,
                msg.emisor,
                "EstadoPrioritarioPerseguir",
                conversationId: msg.conversationId,
                inReplyTo:      msg.replyWith));
            return;
        }

        // Calcular coste: distancia euclidea hasta el destino de la tarea
        float coste = Vector3.Distance(transform.position, destino);

        // Registrar la conversación como CONTRATISTA (primera propuesta que mandamos fija la conv)
        if (conv.Fase == FaseConversacion.Idle)
        {
            conv.Reset();
            conv.ConversationId = msg.conversationId;
            conv.Rol            = RolConversacion.Contratista;
            conv.Fase           = FaseConversacion.CFP;
            conv.Gestor         = msg.emisor;
        }

        // Enviar propuesta
        string contenidoPropuesta = $"{tarea}|{coste:F2}";
        EnviarMensaje(new MensajeFIPA(
            Performativa.Propose,
            com.nombreAgente,
            msg.emisor,
            contenidoPropuesta,
            posicion:       transform.position,
            conversationId: msg.conversationId,
            inReplyTo:      msg.replyWith));

        conv.Fase = FaseConversacion.Propose;
        Debug.Log($"[CapaCom][{com.nombreAgente}] PROPOSE → {msg.emisor} tarea={tarea} coste={coste:F1}");
    }

    private void OnAcceptProposalRecibido(MensajeFIPA msg)
    {
        if (!EsMiConversacion(msg)) return;
        if (conv.Rol != RolConversacion.Contratista) return;

        if (!TryParseTareaDesdeCFP(msg.contenido, out Tareas tarea, out Vector3 destino))
        {
            Debug.LogWarning($"[CapaCom][{com.nombreAgente}] AcceptProposal con contenido inválido.");
            return;
        }

        conv.TareaAsignada = tarea;
        conv.DestinoTarea  = destino;
        conv.Fase          = FaseConversacion.Ejecutando;

        Debug.Log($"[CapaCom][{com.nombreAgente}] ACCEPT recibido — ejecutando tarea={tarea} destino={destino}");
        EjecutarTareaAsignada(tarea, destino);
    }

    private void OnRejectProposalRecibido(MensajeFIPA msg)
    {
        if (!EsMiConversacion(msg)) return;
        if (conv.Rol != RolConversacion.Contratista) return;

        Debug.Log($"[CapaCom][{com.nombreAgente}] REJECT recibido — volviendo a Idle.");
        conv.Reset();
    }

    // EJECUCIÓN DE TAREAS
    private void EjecutarTareaAsignada(Tareas tarea, Vector3 destino)
    {
        switch (tarea)
        {
            case Tareas.BloquearSalidaPrimaria:
            case Tareas.BloquearSalidaSecundaria:
                // Cambiamos al estado BloquearSalida, que notificará cuando llegue
                cerebro.CambiarEstado(new BloquearSalida(destino, this));
                break;

            case Tareas.Buscar:
                // Actualizamos la posición del ladrón en el modelo y vamos a buscar
                cerebro.Modelo.ActualizarSonido(destino);
                cerebro.Deliberativo.EstablecerObjetivo(Objetivo.Buscar);
                StartCoroutine(EsperarFinBusqueda());
                break;
        }
    }

    private System.Collections.IEnumerator EsperarFinBusqueda()
    {
        // Espera hasta que el deliberativo salga de Buscar
        yield return new WaitUntil(() => cerebro.Deliberativo.ObjetivoActual != Objetivo.Buscar);

        if (conv.Fase == FaseConversacion.Ejecutando && conv.Rol == RolConversacion.Contratista)
        {
            NotificarTareaCompletada();
        }
    }

    // HELPERS DE SERIALIZACIÓN

    /// Codifica tarea y destino en el campo 'contenido' del mensaje FIPA.
    private static string CodificarTarea(Tareas tarea, Vector3 destino) =>
        $"{tarea}|{destino.x:F3},{destino.y:F3},{destino.z:F3}";

    /// Decodifica el campo 'contenido' del CFP / AcceptProposal.
    private static bool TryParseTareaDesdeCFP(string contenido, out Tareas tarea, out Vector3 destino)
    {
        tarea   = Tareas.Buscar;
        destino = Vector3.zero;

        if (string.IsNullOrEmpty(contenido)) return false;
        string[] partes = contenido.Split('|');
        if (partes.Length != 2) return false;
        if (!System.Enum.TryParse(partes[0], out tarea)) return false;

        string[] coords = partes[1].Split(',');
        if (coords.Length != 3) return false;
        if (!float.TryParse(coords[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float x)) return false;
        if (!float.TryParse(coords[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float y)) return false;
        if (!float.TryParse(coords[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float z)) return false;

        destino = new Vector3(x, y, z);
        return true;
    }

    /// Decodifica el contenido de un Propose: "TAREA|coste"
    private static bool TryParsePropuesta(string contenido, out Tareas tarea, out float coste)
    {
        tarea = Tareas.Buscar;
        coste = float.MaxValue;

        if (string.IsNullOrEmpty(contenido)) return false;
        string[] partes = contenido.Split('|');
        if (partes.Length != 2) return false;
        if (!System.Enum.TryParse(partes[0], out tarea)) return false;
        return float.TryParse(partes[1], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out coste);
    }


    // HELPERS DE MENSAJERÍA Y GEOMETRÍA

    private void EnviarMensaje(MensajeFIPA msg) => com.EnviarMensaje(msg);

    private bool EsMiConversacion(MensajeFIPA msg) =>
        msg.conversationId == conv.ConversationId || msg.inReplyTo == conv.ConversationId;

    /// Ordena las puertas por distancia al punto dado y devuelve sus posiciones.
    private List<Vector3> ObtenerPuertasOrdenadasPorDistancia(Vector3 origen)
    {
        if (puertas == null || puertas.Length == 0) return new List<Vector3>();

        var lista = new List<(float dist, Vector3 pos)>();
        foreach (Transform puerta in puertas)
        {
            if (puerta == null) continue;
            lista.Add((Vector3.Distance(origen, puerta.position), puerta.position));
        }
        lista.Sort((a, b) => a.dist.CompareTo(b.dist));

        var resultado = new List<Vector3>();
        foreach (var item in lista) resultado.Add(item.pos);
        return resultado;
    }
}