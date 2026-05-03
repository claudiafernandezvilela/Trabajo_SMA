using UnityEngine;
public class ConvQueryIf : Conversacion
{
    public int     RespuestasEsperadas { get; set; }  // nº de agentes contactados
    public int     RespuestasRecibidas { get; set; }  // InformIf recibidos
    public Vector3 MejorPosicion       { get; set; }  // posición más reciente con "true"
    public float   MejorTimestamp      { get; set; }  // timestamp de esa posición
    public float   Deadline            { get; set; }  // timeout de 1 segundo

    public ConvQueryIf(string conversationId) : base(conversationId) { }

    public override bool BloqueaAgente() => Time.time < Deadline;
}
