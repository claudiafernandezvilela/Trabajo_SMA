using System;
using UnityEngine;

/// <summary>
/// Performativas FIPA-ACL estándar.
/// </summary>
public enum Performativa
{
    Inform,
    InformIf,
    InformDone,
    QueryIf,
    QueryRef,
    Request,
    Agree,
    Refuse,
    Failure,
    CFP,
    Propose,
    AcceptProposal,
    RejectProposal,
    NotUnderstood
}

/// <summary>
/// Representa un mensaje según el estándar FIPA-ACL.
/// </summary>
[Serializable]
public class MensajeFIPA
{
    public Performativa performativa;
    public string emisor;
    public string receptor;       // null = broadcast
    public string contenido;
    public Vector3 posicion;      // Vector3.zero si no aplica
    public string conversationId;
    public string replyWith;
    public string inReplyTo;
    public float timestamp;

    public MensajeFIPA(
        Performativa performativa,
        string emisor,
        string receptor,
        string contenido,
        Vector3 posicion      = default,
        string conversationId = null,
        string replyWith      = null,
        string inReplyTo      = null)
    {
        this.performativa   = performativa;
        this.emisor         = emisor;
        this.receptor       = receptor;
        this.contenido      = contenido;
        this.posicion       = posicion;
        this.conversationId = conversationId ?? Guid.NewGuid().ToString("N").Substring(0, 8);
        this.replyWith      = replyWith;
        this.inReplyTo      = inReplyTo;
        this.timestamp      = Time.time;
    }

    public override string ToString() =>
        $"[{performativa}] {emisor} → {receptor ?? "broadcast"} | \"{contenido}\" | conv:{conversationId}";
}