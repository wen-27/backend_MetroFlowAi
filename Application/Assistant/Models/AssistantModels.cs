namespace Application.Assistant.Models;

public sealed record AssistantOption(int Number, string Label);
public sealed record AssistantResponse(string SessionId, string Message, string CurrentStep, IReadOnlyList<AssistantOption> Options, object? Data = null);
public sealed record AssistantStepRequest(string SessionId, int SelectedOption);

