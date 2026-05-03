public class ConvRequest : Conversacion
{
    public bool  Iniciador         { get; }
    public int   RespuestasEsperadas { get; set; }
    public int   RespuestasRecibidas { get; set; }
    public bool  AlguienAcepto       { get; set; }
    public float Deadline            { get; set; }

    public ConvRequest(string conversationId, bool Iniciador) : base(conversationId)
    {
        Iniciador = Iniciador;
    }
    public override bool BloqueaAgente() => !Iniciador;
}
