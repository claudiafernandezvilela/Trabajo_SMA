public enum FaseContractNet
{
    Idle,
    CFP,          // Gestor emite CFP / Contratista lo recibe y evalúa
    Propose,      // Contratista envía propuesta / Gestor acumula respuestas
    Adjudicando,  // Gestor evalúa, asigna y espera InformDone
    Ejecutando    // Contratista ganador ejecuta la tarea
}

/// OnEntrar() se llama UNA SOLA VEZ al entrar en el estado: aquí van los envíos de mensajes.
/// Ejecutar() se llama cada frame: aquí solo va lógica de espera o comprobación de condiciones.
/// OnMensaje() reacciona a mensajes entrantes.
public interface IEstadoConversacion
{
    void OnEntrar(CapaComunicacion capa, Conversacion conv);
    void Ejecutar(CapaComunicacion capa, Conversacion conv);
    void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg);
}
