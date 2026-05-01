public enum FaseContractNet
{
    Idle,
    CFP,           // Se emitió convocatoria — gestor espera propuestas
    Propose,       // Se emitió propuesta    — contratista espera respuesta
    AcceptProposal,// Se aceptó propuesta    — contratista ejecutando
    Inform,        // Tarea completada       — gestor recibe resultado
    Failure        // Tarea fallida          — gestor recibe fallo
} 

public interface IEstadoConversacion
{
    void Ejecutar(CapaComunicacion comunicativo);
}