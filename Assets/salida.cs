using UnityEngine;

public class Salida : MonoBehaviour
{
private void OnTriggerEnter(Collider other)
{
    Debug.Log("Trigger tocado por: " + other.name + " tag: " + other.tag);
    if (!other.transform.root.CompareTag("Player"))
{
    Debug.Log("No es el player, es: " + other.name);
    return;
}
Debug.Log("Root: " + other.transform.root.name);
    if (!other.CompareTag("Player")) return;

    if (Objeto.fueRecogido)
    {
        Debug.Log("¡Victoria!");
        Time.timeScale = 0f;
    }
}
}