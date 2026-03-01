using UnityEngine;

public class Vision : Sensores
{
    [Header("Vision Settings")]
    public float visionRange = 10f;
    [Range(0, 360)]
    public float visionAngle = 45f;
    public LayerMask obstacleLayer;

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
        if (player == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si está muy cerca, lo ve siempre
        if (distanceToPlayer < 1.5f)
        {
            cerebro.OnPlayerSeen(player);
            return;
        }

        if (distanceToPlayer <= visionRange)
        {
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= visionAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up,
                                     directionToPlayer,
                                     distanceToPlayer,
                                     obstacleLayer))
                {
                    cerebro.OnPlayerSeen(player);
                    return;
                }
            }
        }

        cerebro.OnPlayerLost();
    }

    void OnDrawGizmos()
{
    Vector3 origin = transform.position + Vector3.up;

    // Circulo del rango
    Gizmos.color = Color.white;
    Gizmos.DrawWireSphere(transform.position, visionRange);

    // Circulo de detección cercana
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, 1.5f);

    // Líneas del cono
    Vector3 leftBoundary  = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
    Vector3 rightBoundary = Quaternion.Euler(0,  visionAngle / 2f, 0) * transform.forward;

    Gizmos.color = Color.cyan;
    Gizmos.DrawRay(origin, leftBoundary  * visionRange);
    Gizmos.DrawRay(origin, rightBoundary * visionRange);

    // Arco del cono (dibuja segmentos entre los dos rayos)
    int segments = 20;
    float step = visionAngle / segments;
    Vector3 prev = origin + Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;

    for (int i = 1; i <= segments; i++)
    {
        Vector3 next = origin + Quaternion.Euler(0, -visionAngle / 2f + step * i, 0) * transform.forward * visionRange;
        Gizmos.DrawLine(prev, next);
        prev = next;
    }

    // Cambia color si el jugador está dentro del cono
    if (Application.isPlaying && player != null)
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        bool enRango = distanceToPlayer <= visionRange;
        bool enAngulo = angle <= visionAngle / 2f;
        bool sinObstaculo = !Physics.Raycast(origin, directionToPlayer, distanceToPlayer, obstacleLayer);

        if (distanceToPlayer < 1.5f || (enRango && enAngulo && sinObstaculo))
        {
            // Jugador detectado: rojo
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        }
        else
        {
            // Jugador no detectado: verde transparente
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        }

        // Relleno del cono con triángulos
        Vector3 prevFill = origin + Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 nextFill = origin + Quaternion.Euler(0, -visionAngle / 2f + step * i, 0) * transform.forward * visionRange;
            Gizmos.DrawLine(origin, prevFill);
            Gizmos.DrawLine(prevFill, nextFill);
            prevFill = nextFill;
        }
    }
}
}
