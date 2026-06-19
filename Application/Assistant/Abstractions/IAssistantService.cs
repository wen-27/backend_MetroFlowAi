using Application.Assistant.Models;

namespace Application.Assistant.Abstractions;

public interface IAssistantService
{
    Task<AssistantResponse> StartAsync(CancellationToken cancellationToken = default);
    Task<AssistantResponse> StepAsync(AssistantStepRequest request, CancellationToken cancellationToken = default);
}

