using UnityEngine;

public class Objeto : MonoBehaviour
{
    public static bool fueRecogido = false;

    public void Recoger()
    {
        fueRecogido = true;
        gameObject.SetActive(false);
        Debug.Log("Objeto recogido");
    }
}
