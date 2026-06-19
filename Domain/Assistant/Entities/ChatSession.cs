using Domain.Common;

namespace Domain.Assistant.Entities;

public sealed class ChatSession : Entity
{
    public string SessionKey { get; set; } = Guid.NewGuid().ToString("N");
    public string CurrentStep { get; set; } = "MainMenu";
    public Guid? OriginStationId { get; set; }
    public Guid? DestinationStationId { get; set; }
    public Guid? SelectedRouteId { get; set; }
    public int? LastOptionSelected { get; set; }
    public bool IsCompleted { get; set; }
}

