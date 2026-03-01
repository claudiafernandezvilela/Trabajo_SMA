using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float height = 10f;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y + height, target.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(90, 0, 0); // Mirando hacia abajo
    }
}
