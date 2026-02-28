using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class AbrirPuerta : MonoBehaviour
{
    public float velocidad = 2f;
    public float anguloAbierta = 150f;
    public BoxCollider colliderBloqueo;
    public NavMeshObstacle navMeshObstacle;

    private bool abierta = false;
    private bool jugadorCerca = false;
    private bool agenteCerca = false;
    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;

    public bool EstaAbierta => abierta;

    void Start()
    {
        rotacionCerrada = transform.rotation;
        rotacionAbierta = Quaternion.Euler(transform.eulerAngles + new Vector3(0, anguloAbierta, 0));
    }

    void Update()
    {
        // Jugador abre con E
        if (jugadorCerca && Keyboard.current.eKey.wasPressedThisFrame)
            Abrir();

        if (abierta)
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionAbierta, Time.deltaTime * velocidad);
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionCerrada, Time.deltaTime * velocidad);
    }

    public void Abrir()
    {
        abierta = true;
        colliderBloqueo.enabled = false;
        navMeshObstacle.enabled = false;
    }

    public void Cerrar()
    {
        abierta = false;
        colliderBloqueo.enabled = true;
        navMeshObstacle.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) jugadorCerca = true;
        if (other.CompareTag("Agente")) agenteCerca = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) jugadorCerca = false;
        if (other.CompareTag("Agente"))
        {
            agenteCerca = false;
            Cerrar(); // cierra al salir el agente
        }
    }
}