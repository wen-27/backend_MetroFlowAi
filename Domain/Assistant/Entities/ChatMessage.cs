using Domain.Common;

namespace Domain.Assistant.Entities;

public sealed class ChatMessage : Entity
{
    public Guid ChatSessionId { get; set; }
    public ChatSender Sender { get; set; } = ChatSender.System;
    public string Message { get; set; } = "";
    public int? SelectedOption { get; set; }
}

