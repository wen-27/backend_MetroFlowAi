using Application.Assistant.Abstractions;
using Application.Assistant.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Assistant.Controllers;

[ApiController]
[Route("api/assistant")]
public sealed class AssistantController(IAssistantService assistant) : ControllerBase
{
    [HttpPost("start")]
    public Task<AssistantResponse> Start(CancellationToken cancellationToken) =>
        assistant.StartAsync(cancellationToken);

    [HttpPost("step")]
    public Task<AssistantResponse> Step([FromBody] AssistantStepRequest request, CancellationToken cancellationToken) =>
        assistant.StepAsync(request, cancellationToken);
}

