using System.Collections.Generic;
using UnityEngine;

/// Singleton de escena que registra todas las salidas del edificio.
/// Coloca este componente en un GameObject vacío de la escena (p.ej. "GestorEscena")
/// y arrastra en el inspector todos los Transforms que representan salidas.
///
/// Los agentes policía NO necesitan una referencia en el inspector:
/// acceden mediante RegistroSalidas.Instancia desde cualquier script.
public class RegistroSalidas : MonoBehaviour
{
    // ── inspector ──────────────────────────────────────────────────────────
    [Header("Salidas del edificio")]
    [Tooltip("Arrastra aquí todos los Transforms de salida de la escena.")]
    public Transform[] salidas;

    // ── singleton ──────────────────────────────────────────────────────────
    public static RegistroSalidas Instancia { get; private set; }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Debug.LogWarning("[RegistroSalidas] Instancia duplicada eliminada.");
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    // ── API ────────────────────────────────────────────────────────────────

    /// Devuelve las N salidas más cercanas a una posición dada,
    /// ordenadas de menor a mayor distancia.
    /// Si hay menos de N salidas registradas devuelve todas las disponibles.
    public List<Transform> ObtenerMasCercanas(Vector3 origen, int n)
    {
        if (salidas == null || salidas.Length == 0)
        {
            Debug.LogWarning("[RegistroSalidas] No hay salidas registradas.");
            return new List<Transform>();
        }

        // Copia y ordena por distancia ascendente
        var ordenadas = new List<Transform>(salidas);
        ordenadas.Sort((a, b) =>
            Vector3.Distance(origen, a.position)
            .CompareTo(Vector3.Distance(origen, b.position)));

        // Devuelve hasta n elementos
        int cantidad = Mathf.Min(n, ordenadas.Count);
        return ordenadas.GetRange(0, cantidad);
    }
}