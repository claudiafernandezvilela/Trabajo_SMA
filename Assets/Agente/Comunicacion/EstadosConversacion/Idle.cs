public class EstadoIdle : IEstadoConversacion
{
    public void OnEntrar(CapaComunicacion capa, ConversacionContractNet conv) { }
    public void Ejecutar(CapaComunicacion capa, ConversacionContractNet conv) { }
    public void OnMensaje(CapaComunicacion capa, ConversacionContractNet conv, MensajeFIPA msg) { }
}