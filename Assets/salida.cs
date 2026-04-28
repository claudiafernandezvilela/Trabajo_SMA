using UnityEngine;

public class SalidaMapa : MonoBehaviour
{
    public float radioSalida = 3f;
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        float distancia = Vector3.Distance(transform.position, player.position);
        if (distancia <= radioSalida && Objeto.fueRecogido)
        {
            Debug.Log("Game Over - El ladrón escapó con el objeto");
            Time.timeScale = 0f;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioSalida);
    }
}