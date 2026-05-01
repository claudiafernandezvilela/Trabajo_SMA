// Helper de transición
// Centraliza la llamada a SetEstado + OnEntrar para no repetirlo en cada estado.
public static class Transicion
{
    public static void A(CapaComunicacion capa, ConversacionContractNet conv,
                         IEstadoConversacion nuevo, FaseContractNet fase)
    {
        conv.SetEstado(nuevo, fase);
        nuevo.OnEntrar(capa, conv);
    }
}