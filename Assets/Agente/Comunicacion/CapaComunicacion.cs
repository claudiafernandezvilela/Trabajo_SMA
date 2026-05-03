using System.Collections.Generic;
using UnityEngine;

public class CapaComunicacion : MonoBehaviour
{
    public ProcesarMensajes Mensajes { get; private set; }
    public string NombreAgente => Mensajes.nombreAgente;

    private Cerebro cerebro;
    private CerebroDeliberativo deliberativo;

    // conversaciones activas → Clave: conversationId. Valor: cualquier protocolo (CNet, QueryIf, Request).
    private readonly Dictionary<string, Conversacion> conversaciones
        = new Dictionary<string, Conversacion>();

    // estado de visión
    // Flag que evita lanzar múltiples CFPs simultáneos cuando el agente está viendo al ladrón.
    private bool cfpVisionActivo = false;
    // Timestamp del último broadcast enviado. Inicializado en -999 para que el primero siempre pase el filtro.
    private float tiempoUltimaBroadcastPosicion = -999f;
    //Throttle de 0.5 segundos entre broadcasts de posición, para no saturar la red.
    private const float IntervaloMinBroadcast = 0.5f;

    void Awake()
    {
        Mensajes = GetComponent<ProcesarMensajes>();
        cerebro = GetComponent<Cerebro>();
        deliberativo = GetComponent<CerebroDeliberativo>();
    }

    void Update()
    {
        var snapshot = new List<Conversacion>(conversaciones.Values);
        foreach (var conv in snapshot)
        {
            conv.EstadoActual?.Ejecutar(this, conv);
            if (conv.Terminada)
                conversaciones.Remove(conv.ConversationId);
        }
    }

    // Eventos
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

    public void NotificarLadronVisto(Vector3 posicion)
    {
        if (Time.time - tiempoUltimaBroadcastPosicion >= IntervaloMinBroadcast)
        {
            tiempoUltimaBroadcastPosicion = Time.time;
            Mensajes.EnviarMensaje(new MensajeFIPA(
                Performativa.Inform, NombreAgente, null, "ladron_visto", posicion));
        }

        if (cfpVisionActivo || HayConversacionActivaComoGestor()) return;
        cfpVisionActivo = true;

        var (s1, s2) = ObtenerSalidasCercanas(posicion);
        IniciarCFP(new List<TareaData>
        {
            new TareaData(TipoTarea.BloquearSalida1, posicion, s1),
            new TareaData(TipoTarea.BloquearSalida2, posicion, s2)
        }, gestorCompite: false);
    }

    public void NotificarLadronPerdido()
    {
        cfpVisionActivo = false;
    }

    public void NotificarObjetoRobadoBroadcast()
    {
        Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.Inform, NombreAgente, null, "objeto_robado", Vector3.zero));
        Debug.Log($"[{NombreAgente}] Broadcast: objeto_robado.");
    }

    // Lógica del Contract Net
    private void IniciarCFP(List<TareaData> tareas, bool gestorCompite)
    {
        string convId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        var conv = new ConvCNet(convId, RolContractNet.Gestor);
        conv.GestorId = NombreAgente;
        conv.GestorCompite = gestorCompite;
        foreach (var t in tareas) conv.TareasDisponibles.Add(t);
        conversaciones[convId] = conv;
        Transicion.A(this, conv, new EstadoCFP(), FaseContractNet.CFP);
        Debug.Log($"[{NombreAgente}] CFP conv:{convId} tareas:{tareas.Count} gestorCompite:{gestorCompite}");
    }

    private void RecibirCFP(MensajeFIPA msg)
    {
        if (conversaciones.ContainsKey(msg.conversationId))
        {
            Debug.LogWarning($"[{NombreAgente}] CFP duplicado conv:{msg.conversationId}");
            return;
        }
        var conv = new ConvCNet(msg.conversationId, RolContractNet.Contratista);
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

    // Lógica QueryIf
    public void IniciarQuery()
    {
        string convId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        var conv = new ConvQueryIf(convId);
        conversaciones[convId] = conv;
        Transicion.A(this, conv, new QueryIf());
    }

    private void ResponderQuery(MensajeFIPA query)
    {
        const float umbralSegundos = 5f;
        bool tieneInfo = cerebro.Modelo.ultimaPosicionJugador != Vector3.zero
                         && (Time.time - cerebro.Modelo.tiempoUltimaPosicion) <= umbralSegundos;

        Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.InformIf,
            NombreAgente,
            query.emisor,
            tieneInfo ? "true" : "false",
            tieneInfo ? cerebro.Modelo.ultimaPosicionJugador : Vector3.zero,
            query.conversationId));

        Debug.Log($"[{NombreAgente}] InformIf ladron_cercano={tieneInfo} → {query.emisor}");
    }

    public void OnResultadoQueryBusqueda(bool encontrado, Vector3 posicion)
    {
        cerebro.OnResultadoQueryBusqueda(encontrado, posicion);
    }

    // LógicaRequest
    public void IniciarRequestAsegurar()
    {
        string convId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        var conv = new ConvRequest(convId, Iniciador: true);
        conversaciones[convId] = conv;
        Transicion.A(this, conv, new Request());
    }

    private void ResponderRequest(MensajeFIPA request)
    {
        if (EstaOcupado())
        {
            Mensajes.EnviarMensaje(new MensajeFIPA(
                Performativa.Refuse,
                NombreAgente,
                request.emisor,
                "ocupado",
                Vector3.zero,
                request.conversationId));
            Debug.Log($"[{NombreAgente}] Refuse Request asegurar_zona → ocupado");
            return;
        }

        Mensajes.EnviarMensaje(new MensajeFIPA(
            Performativa.Agree,
            NombreAgente,
            request.emisor,
            "asegurar_zona",
            Vector3.zero,
            request.conversationId));

        deliberativo.EstablecerObjetivo(Objetivo.BarrerMapa);

        var conv = new ConvRequest(request.conversationId, Iniciador: false);
        conv.InterlocutorId = request.emisor;
        conversaciones[request.conversationId] = conv;
        Transicion.A(this, conv, new EjecutarRequest());
        Debug.Log($"[{NombreAgente}] Agree Request asegurar_zona → {request.emisor}");
    }

    public void NotificarAsegurarZonaCompletada()
    {
        foreach (var conv in conversaciones.Values)
        {
            if (conv is ConvRequest req && !req.Iniciador
                && conv.EstadoActual is EjecutarRequest ejec)
            {
                ejec.NotificarCompletada(this, conv);
                return;
            }
        }
    }

    public void NotificarTareaCompletada()
    {
        foreach (var conv in conversaciones.Values)
        {
            if (conv is ConvCNet cnet
                && cnet.Fase == FaseContractNet.Ejecutando
                && cnet.EstadoActual is EstadoEjecutando ejec)
            {
                ejec.NotificarCompletada(this, cnet);
                return;
            }
        }
    }

    // Lógica de todos lo mensajes recibidos
    public void OnMensajeRecibido(MensajeFIPA msg)
    {
        if (msg.performativa == Performativa.Inform &&
            (msg.contenido == "ladron_escuchado" ||
             msg.contenido == "ladron_visto" ||
             msg.contenido == "objeto_robado"))
            return;
        if (msg.performativa == Performativa.CFP)
        {
            RecibirCFP(msg);
            return;
        }
        if (msg.performativa == Performativa.QueryIf && msg.contenido == "ladron_cercano")
        {
            ResponderQuery(msg);
            return;
        }
        if (msg.performativa == Performativa.Request && msg.contenido == "asegurar_zona")
        {
            ResponderRequest(msg);
            return;
        }
        if (!conversaciones.TryGetValue(msg.conversationId, out var conv))
        {
            if (msg.performativa != Performativa.InformDone)
                Debug.LogWarning($"[{NombreAgente}] Mensaje para conv desconocida:{msg.conversationId}");
            return;
        }
        conv.EstadoActual?.OnMensaje(this, conv, msg);

        if (conv.Terminada)
            conversaciones.Remove(conv.ConversationId);
    }

    // callback hacia Cerebro
    public void OnTareaAsignada(TareaData tarea)
    {
        // Debug.Log($"[{NombreAgente}] Tarea asignada: {tarea.tipo} destino:{tarea.DestinoEjecucion}");
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

    /// True si el agente está comprometido con una conversación que le impide
    public bool EstaOcupado()
    {
        if (deliberativo.ObjetivoActual == Objetivo.Perseguir) return true;
        foreach (var conv in conversaciones.Values)
            if (conv.BloqueaAgente()) return true;
        return false;
    }

    private bool HayConversacionActivaComoGestor()
    {
        foreach (var conv in conversaciones.Values)
            if (conv is ConvCNet cnet
                && cnet.Rol == RolContractNet.Gestor
                && !cnet.Terminada)
                return true;
        return false;
    }

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