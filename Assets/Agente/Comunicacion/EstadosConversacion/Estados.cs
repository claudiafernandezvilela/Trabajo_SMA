// Enum de fases
public enum FaseContractNet
{
    Idle,
    CFP,          // Gestor emite CFP / Contratista lo recibe y evalúa
    Propose,      // Contratista envía propuesta / Gestor acumula respuestas
    Adjudicando,  // Gestor evalúa y decide
    Ejecutando    // Contratista ganador ejecuta la tarea
}

// Interfaz
/// OnEntrar() se llama UNA SOLA VEZ al entrar en el estado: aquí van los envíos de mensajes.
/// Ejecutar() se llama cada frame: aquí solo va lógica de espera o comprobación de condiciones.
/// OnMensaje() reacciona a mensajes entrantes.
public interface IEstadoConversacion
{
    void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv);
    void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv);
    void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg);
}
