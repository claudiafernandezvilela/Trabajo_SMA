using UnityEngine;

public class ModeloMundo : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Vector3 ultimaPosicionJugador;
    public bool jugadorVisible;
    public bool jugadorEscuchado;

    [Header("Object")]
    public Transform objetoVigilado;
    public Vector3 posicionObjeto;
    public bool objetoRobado => Objeto.fueRecogido;

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
}