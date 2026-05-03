// Helper de transición
// Centraliza la llamada a SetEstado + OnEntrar para no repetirlo en cada estado.
public static class Transicion
{
    public static void A(CapaComunicacion capa, ConvCNet cnet,
                         IEstadoConversacion nuevo, FaseContractNet fase)
    {
        cnet.SetEstado(nuevo, fase);
        nuevo.OnEntrar(capa, cnet);
    }

    /// Generic overload for QueryIf and Request conversations (no FaseContractNet).
    public static void A(CapaComunicacion capa, Conversacion conv, IEstadoConversacion nuevo)
    {
        conv.SetEstado(nuevo);
        nuevo.OnEntrar(capa, conv);
    }
}
