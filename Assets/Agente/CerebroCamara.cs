using UnityEngine;

/// Cerebro mínimo para una cámara de seguridad fija.
/// No tiene capa deliberativa ni reactiva: la única acción posible es
/// notificar al resto de agentes cuando detecta al ladrón (via Vision).
/// La decisión es puramente reactiva: ver_ladrón → disparar ContractNet.
public class CerebroCamara : Cerebro
{
    private CapaComunicacion capaCom;

    // Cerebro.Awake() se ejecuta y asigna Modelo correctamente.
    new void Start()
    {
        capaCom = GetComponent<CapaComunicacion>();
        // Sin estado inicial: la cámara no patrulla ni ejecuta comportamientos.
    }

    new void Update() { }

    public override void OnPlayerSeen(Transform playerTransform)
    {
        Modelo.ActualizarJugadorVisto(playerTransform);
        capaCom.NotificarLadronVisto(playerTransform.position);
    }

    public override void OnPlayerLost()
    {
        Modelo.ActualizarJugadorPerdido();
        capaCom.NotificarLadronPerdido();
    }

    public override void ObjetoRobado()
    {
        Modelo.objetoRobado = true;
    }
}
