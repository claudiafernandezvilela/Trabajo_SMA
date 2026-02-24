using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform target;
    public float height = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        // Sigue la posición del personaje
        transform.position = new Vector3(
            target.position.x,
            target.position.y + height,
            target.position.z
        );

        // Rotación fija mirando hacia abajo
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
