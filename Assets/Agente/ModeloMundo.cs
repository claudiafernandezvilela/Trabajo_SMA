using UnityEngine;

public class ModeloMundo : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Vector3 ultimaPosicionJugador;
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
    }

    public void ActualizarJugadorPerdido()
    {
        jugadorVisible = false;
    }

    public void ActualizarSonido(Vector3 posicion)
    {
        ultimaPosicionJugador = posicion;
        jugadorEscuchado = true;
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
}
