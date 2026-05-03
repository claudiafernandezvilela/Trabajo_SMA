public class EstadoIdle : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, Conversacion conv) { }
    public void Ejecutar(CapaComunicacion capa, Conversacion conv) { }
    public void OnMensaje(CapaComunicacion capa, Conversacion conv, MensajeFIPA msg) { }
}