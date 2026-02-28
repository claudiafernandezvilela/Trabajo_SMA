using UnityEngine;

public class Mundo : MonoBehaviour
{
    public static Mundo Instancia { get; private set; }

    [Header("Object Settings")]
    public Transform objetoVigilado;
    public Vector3 posicionObjeto;
    public bool objetoRobado => Objeto.fueRecogido;

    [Header("Player")]
    public Transform player;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    void Start()
    {
        if (objetoVigilado != null)
            posicionObjeto = objetoVigilado.position;
    }
}
