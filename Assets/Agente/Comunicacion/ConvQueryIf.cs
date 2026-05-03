using UnityEngine;
public class ConvQueryIf : Conversacion
{
    public int     RespuestasEsperadas { get; set; }
    public int     RespuestasRecibidas { get; set; }
    public Vector3 MejorPosicion       { get; set; } = Vector3.zero;
    public float   MejorTimestamp      { get; set; } = -1f;
    public float   Deadline            { get; set; }

    public ConvQueryIf(string conversationId) : base(conversationId) { }

    public override bool BloqueaAgente() => false;
}
