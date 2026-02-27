using UnityEngine;

public class Audicion : Sensores
{
    [Header("Hearing Settings")]
    public float radioEscucha = 10f;

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        Detect();
    }

    protected override void Detect()
    {
        // Detect() aquí no hace nada, la alerta viene de fuera
    }

public void RecibirSonido(Vector3 posicionSonido, float volumen)
{
    float distancia = Vector3.Distance(transform.position, posicionSonido);

    Debug.Log("Distancia: " + distancia + " Radio: " + radioEscucha);

    if (distancia <= radioEscucha)
    {
        cerebro.OnPlayerHeard(posicionSonido);
    }
}

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, radioEscucha);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radioEscucha);
    }
}