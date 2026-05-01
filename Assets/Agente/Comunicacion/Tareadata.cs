using UnityEngine;

/// Tipos de tarea que el gestor puede repartir en un ContractNet.
/// El orden de declaración refleja la prioridad de asignación:
/// el agente con mejor puntuación recibe la tarea de menor índice.
public enum TipoTarea
{
    BloquearSalida1,   // agente más cercano al ladrón → bloquea la salida más próxima al ladrón
    BloquearSalida2,   // segundo agente → bloquea la segunda salida más próxima al ladrón
    Buscar             // agente restante → busca al ladrón en su última posición conocida
}

/// Descripción de una tarea concreta que viaja en el CFP y en el AcceptProposal.
/// Es un value object: solo datos, sin lógica.
/// El gestor calcula posicionSalida antes de emitir el CFP consultando RegistroSalidas;
/// el contratista que recibe AcceptProposal solo necesita ir a posicionSalida.
[System.Serializable]
public class TareaData
{
    /// Tipo de tarea asignada.
    public TipoTarea tipo;

    /// Posición del ladrón en el momento de emitir el CFP.
    /// Todos los contratistas la usan para calcular su puntuación (distancia al ladrón).
    public Vector3 posicionLadron;

    /// Posición de la salida que debe bloquear el agente.
    /// Solo relevante para BloquearSalida1 y BloquearSalida2.
    /// Para la tarea Buscar es Vector3.zero (el agente usa posicionLadron).
    public Vector3 posicionSalida;

    public TareaData(TipoTarea tipo, Vector3 posicionLadron, Vector3 posicionSalida = default)
    {
        this.tipo           = tipo;
        this.posicionLadron = posicionLadron;
        this.posicionSalida = posicionSalida;
    }

    /// Destino efectivo que debe alcanzar el agente para ejecutar esta tarea.
    /// Para bloqueos es la salida; para búsqueda es la última posición del ladrón.
    public Vector3 DestinoEjecucion =>
        (tipo == TipoTarea.Buscar || posicionSalida == Vector3.zero)
            ? posicionLadron
            : posicionSalida;

    // ── serialización ──────────────────────────────────────────────────────
    // Formato: "tipo|lx|ly|lz|sx|sy|sz"

    public string Serializar() =>
        $"{(int)tipo}" +
        $"|{posicionLadron.x}|{posicionLadron.y}|{posicionLadron.z}" +
        $"|{posicionSalida.x}|{posicionSalida.y}|{posicionSalida.z}";

    /// Devuelve null si el formato no es válido.
    public static TareaData Deserializar(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        string[] p = s.Split('|');
        if (p.Length != 7) return null;

        if (!int.TryParse(p[0],   out int   t))  return null;
        if (!float.TryParse(p[1], out float lx)) return null;
        if (!float.TryParse(p[2], out float ly)) return null;
        if (!float.TryParse(p[3], out float lz)) return null;
        if (!float.TryParse(p[4], out float sx)) return null;
        if (!float.TryParse(p[5], out float sy)) return null;
        if (!float.TryParse(p[6], out float sz)) return null;

        return new TareaData(
            (TipoTarea)t,
            new Vector3(lx, ly, lz),
            new Vector3(sx, sy, sz));
    }
}