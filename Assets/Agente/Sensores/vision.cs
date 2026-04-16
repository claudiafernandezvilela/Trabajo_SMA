using UnityEngine;

public class Vision : Sensores
{
    [Header("Vision Settings")]
    public float visionRange = 10f;
    [Range(0, 360)]
    public float visionAngle = 45f;
    public LayerMask obstacleLayer;

    private Transform playerTransformGizmo;

    void Update()
    {
        Detect();
    }

protected override void Detect()
{
    bool playerSeenThisFrame  = false;
    bool objetoVisto          = false;

    Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);

    foreach (Collider hit in hits)
    {
        bool esPlayer   = hit.CompareTag("Player");
        bool esObjeto = hit.CompareTag("object");
        if (!esPlayer && !esObjeto) continue;

        Transform target = hit.transform;
        Vector3 dir  = (target.position - transform.position).normalized;
        float   dist = Vector3.Distance(transform.position, target.position);

        bool visto = false;
        if (dist < 1.5f)
        {
            visto = true;
        }
        else
        {
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > visionAngle / 2f) continue;
            if (Physics.Raycast(transform.position + Vector3.up, dir, dist, obstacleLayer)) continue;
            visto = true;
        }

        if (!visto) continue;

        if (esPlayer)
        {   cerebro.OnPlayerSeen(target);
            playerSeenThisFrame = true;
            playerTransformGizmo = target;
        }
        else if (esObjeto)
        {
            objetoVisto = true;
        }
    }

    if (!playerSeenThisFrame) cerebro.OnPlayerLost();


    // Si la posición conocida del objeto entra en el cono pero no vemos el collider → robado
    if (!objetoVisto && !cerebro.Modelo.objetoRobado && cerebro.Modelo.posicionObjeto != Vector3.zero)
    {
        float dist  = Vector3.Distance(transform.position, cerebro.Modelo.posicionObjeto);
        Vector3 dir = (cerebro.Modelo.posicionObjeto - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        bool enRango  = dist <= visionRange;
        bool enAngulo = angle <= visionAngle / 2f || dist < 1.5f;
        bool sinObstaculo = !Physics.Raycast(transform.position + Vector3.up, dir, dist, obstacleLayer);

        if (enRango && enAngulo && sinObstaculo)
            cerebro.ObjetoRobado(); //cambiar por null en vez de bool
    }
}
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up;

        // Círculo del rango
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Círculo de detección cercana
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

        // Líneas del cono
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, leftBoundary * visionRange);
        Gizmos.DrawRay(origin, rightBoundary * visionRange);

        // Arco del cono
        int segments = 20;
        float step = visionAngle / segments;
        Vector3 prev = origin + Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;

        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = origin + Quaternion.Euler(0, -visionAngle / 2f + step * i, 0) * transform.forward * visionRange;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Relleno del cono según si el jugador es visible
        if (Application.isPlaying && playerTransformGizmo != null)
        {
            Vector3 dirToPlayer = (playerTransformGizmo.position - transform.position).normalized;
            float distToPlayer = Vector3.Distance(transform.position, playerTransformGizmo.position);
            float ang = Vector3.Angle(transform.forward, dirToPlayer);

            bool enRango = distToPlayer <= visionRange;
            bool enAngulo = ang <= visionAngle / 2f;
            bool sinObstaculo = !Physics.Raycast(origin, dirToPlayer, distToPlayer, obstacleLayer);

            bool detectado = distToPlayer < 1.5f || (enRango && enAngulo && sinObstaculo);
            Gizmos.color = detectado
                ? new Color(1f, 0f, 0f, 0.3f)
                : new Color(0f, 1f, 0f, 0.15f);

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
