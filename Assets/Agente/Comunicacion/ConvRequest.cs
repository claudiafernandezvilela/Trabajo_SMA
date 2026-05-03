public class ConvRequest : Conversacion
{
    public bool  EsIniciador         { get; }
    public int   RespuestasEsperadas { get; set; }
    public int   RespuestasRecibidas { get; set; }
    public bool  AlguienAceptó       { get; set; }
    public float Deadline            { get; set; }

    public ConvRequest(string conversationId, bool esIniciador) : base(conversationId)
    {
        EsIniciador = esIniciador;
    }
    public override bool BloqueaAgente() => !EsIniciador;
}
