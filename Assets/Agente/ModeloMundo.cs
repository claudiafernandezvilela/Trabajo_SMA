using UnityEngine;

public class ModeloMundo : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Vector3 ultimaPosicionJugador;
    public float tiempoUltimaPosicion { get; private set; } = -999f;
    public bool jugadorVisible;
    public bool jugadorEscuchado;

    [Header("Object")]
    // La referencia se asigna cuando el agente VE el objeto por primera vez.
    // Si llega a la posición y objetoVigilado sigue siendo null → fue robado.
    public Transform objetoVigilado;
    public Vector3 posicionObjeto;
    public bool objetoRobado;

void Start()
{
    if (objetoVigilado != null)
        posicionObjeto = objetoVigilado.position;
}
    public void ActualizarJugadorVisto(Transform playerTransform)
    {
        player = playerTransform;
        jugadorVisible = true;
        ultimaPosicionJugador = playerTransform.position;
        tiempoUltimaPosicion = Time.time;
    }

    public void ActualizarJugadorPerdido()
    {
        jugadorVisible = false;
    }

    public void ActualizarSonido(Vector3 posicion)
    {
        ultimaPosicionJugador = posicion;
        jugadorEscuchado = true;
        tiempoUltimaPosicion = Time.time;
    }

    // Consume el flag de escucha para evitar que el reactivo lo procese en frames sucesivos (ruido reactivo).
    public void ResetearEscucha()
    {
        jugadorEscuchado = false;
    }
 
    /// Llamado por ObjetoVisible.OnSeen() cada vez que el agente ve el objeto.
    /// Actualiza la referencia y la posición conocida.
    public void ActualizarObjetoVisto(Transform obj)
    {
        objetoVigilado = obj;
        posicionObjeto = obj.position;
    }
 
    /// Llamado por RevisarObjeto cuando el agente llega a la posición y no ve el objeto.
    /// Marca el objeto como "robado" borrando la referencia.
    public void MarcarObjetoComoRobado()
    {
        objetoVigilado = null;
    }

    /// Usa las últimas posiciones del historial para estimar dónde estará el ladrón ahora.
    /// Si hay menos de 2 puntos, devuelve la última posición conocida sin predicción.
    public Vector3 PredecirPosicionLadron(System.Collections.Generic.List<(Vector3 pos, float t)> hist)
    {
    if (hist.Count < 2) return ultimaPosicionJugador;
    Vector3 dir = hist[0].pos - hist[1].pos;
    float dt = hist[0].t - hist[1].t;
    if (dt <= 0f) return ultimaPosicionJugador;
    float tiempoTranscurrido = Time.time - hist[0].t;
    Vector3 prediccion = hist[0].pos + dir / dt * tiempoTranscurrido;

    Debug.DrawLine(hist[0].pos, prediccion, Color.red);
    Debug.DrawRay(prediccion, Vector3.up * 1.5f, Color.red);

    return prediccion;
    }
}
