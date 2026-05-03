/// Clase Base para conversaciones FIPA.
public abstract class Conversacion
{
    public string ConversationId  { get; }
    public string InterlocutorId  { get; set; }
    public IEstadoConversacion EstadoActual { get; private set; }

    public bool Terminada => EstadoActual is EstadoIdle;

    protected Conversacion(string conversationId)
    {
        ConversationId = conversationId;
    }

    public virtual void SetEstado(IEstadoConversacion nuevoEstado)
    {
        EstadoActual = nuevoEstado;
    }
    public abstract bool BloqueaAgente();
}
