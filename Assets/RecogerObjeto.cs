using UnityEngine;
using UnityEngine.InputSystem;

public class RecogerObjeto : MonoBehaviour
{
    [Header("Settings")]
    public float distanciaRecoger = 5f;
    public LayerMask Object;

    void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            IntentarRecoger();
        }
    }

    void IntentarRecoger()
    {
        Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, distanciaRecoger, Object);
        Debug.Log("Objetos encontrados: " + objetosCercanos.Length);

        if (objetosCercanos.Length == 0)
        {
            Debug.Log("No hay objetos en radio " + distanciaRecoger + " con la layer " + Object.value);
            return;
        }

        Collider masCorto = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Collider col in objetosCercanos)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                masCorto = col;
            }
        }

        if (masCorto != null)
        {
            Objeto objeto = masCorto.GetComponent<Objeto>();
            if (objeto != null)
            {
                objeto.Recoger();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, distanciaRecoger);
    }
}